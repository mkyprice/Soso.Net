using System;
using System.Collections.Generic;
using Soso.Net.Behaviors;
using Soso.Net.Behaviors.Rpc;
using Soso.Net.Components.NetworkedBodies;
using Soso.Net.Components.NetworkedBodies.Helpers;
using Soso.Net.Components.NetworkedBodies.Snapshots;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Utils;
using Soso.Utils.Concurrency;
using UnityEngine;

namespace Soso.Net.Components.PredictedBodies
{
    public class PredictedRigidBody2D : INetworkReceiver
    {
        [SerializeField, Range(0.0001f, 1f)] public float PositionSensitivity = 0.05f;
        [SerializeField, Range(0.0001f, 1f)] public float RotationSensitivity = 0.05f;
        [SerializeField, Range(0.0001f, 1f)] public float LinearVelocitySensitivity = 0.05f;
        [SerializeField, Range(0.0001f, 1f)] public float AngularVelocitySensitivity = 0.05f;
        [SerializeField, Range(0, 1f)] public float RestThreshold = 0.1f;
        [SerializeField, Min(2)] public int HistoryLimit = 32;
        [SerializeField, Range(20, 200)] public long RecordingIntervalMs = 50;
        [SerializeField] public bool OnlySyncOnChange = false;
        [SerializeField, Range(20, 5000)] public long SendIntervalMs = 500;
        [SerializeField, Range(0, 60)] public long MinSendIntervalMs = 20;
        [SerializeField] public bool AllowAuthoritySwap = false;
        [SerializeField, Range(0, 1000)] public long AuthorityDropDelayMs = 200;
        [SerializeField, Range(100, 5000)] public long AuthorityHeartbeatMs = 1000;
        [SerializeField, Range(100, 5000)] public long AuthorityHeartbeatTimeoutMs = 2500;
        [SerializeField] public bool Reliable;
        
        public Rigidbody2D Rb;

        private Dictionary<ushort, long> _collisions = new Dictionary<ushort, long>();
        private SortedList<long, RigidBody2DState> _history = new SortedList<long, RigidBody2DState>();
        private RigidBody2DState _lastSentState;
        private long _latestProcessedReceiveState;
        private SosoConcurrentQueue<RigidBody2DState> _receivedStates = new SosoConcurrentQueue<RigidBody2DState>();
        private long _lastSendTime;
        private long _lastRecordTime;
        
        // Authority
        private int _localContactCount;
        private bool _pendingAuthGiveUp;
        private long _contactExitTime;
        private ushort _pendingGiveUpOwnerId;
        private long _lastAuthHeartbeatTime;

        private void Awake()
        {
            if (Rb == false)
            {
                Rb = GetComponent<Rigidbody2D>();
            }
        }

        private void OnDisable()
        {
            _pendingAuthGiveUp = false;
            _lastSendTime = 0;
            _lastRecordTime = 0;
            _localContactCount = 0;
            _latestProcessedReceiveState = 0;
            _receivedStates.Clear();
            _history.Clear();
            _collisions.Clear();
        }

        private void FixedUpdate()
        {
            if (IsInitialized == false)
            {
                return;
            }
            var time = NetworkTime.Time;
            bool isAuthority = NetId.IsAuthority();
            
            if (isAuthority)
            {
                var state = BuildState(time);
                if ((OnlySyncOnChange == false && IsSendReady()) || (IsMinSendReady() && HasChanged(state, _lastSentState)))
                {
                    _lastSendTime = time;
                    SendUpdate(state);
                }
            }
            else
            {
                RigidBody2DState? latestState = null;
                while (_receivedStates.TryDequeue(out var receivedState))
                {
                    if (receivedState.Timestamp > _latestProcessedReceiveState)
                    {
                        _latestProcessedReceiveState = receivedState.Timestamp;
                        latestState = receivedState;
                    }
                }
                if (latestState.HasValue)
                {
                    ReceiveState(latestState.Value);
                }
            }
            
            if (IsRecordReady() || _history.Count < 2)
            {
                RecordState();
            }

            if (_pendingAuthGiveUp && time >= _contactExitTime + AuthorityDropDelayMs)
            {
                _pendingAuthGiveUp = false;
                Rpc(RpcRequestGiveUpOwnership, _pendingGiveUpOwnerId, BuildState(time));
            }
            if (IsAuthority() && _localContactCount > 0 && time >= _lastAuthHeartbeatTime + AuthorityHeartbeatMs)
            {
                _lastAuthHeartbeatTime = time;
                Rpc(RpcRequestOwnership, INetworkManager.SessionId);
            }
            
            // Auth pruning
            if (NetId.IsServer && _collisions.Count > 0)
            {
                PruneInactiveAuthorityCollisions(time);
            }
        }

        private bool IsRecordReady()
        {
            var time = NetworkTime.Time;
            return _lastRecordTime + RecordingIntervalMs <= time;
        }

        private bool IsSendReady()
        {
            var time = NetworkTime.Time;
            return _lastSendTime + SendIntervalMs <= time;
        }

        private bool IsMinSendReady()
        {
            var time = NetworkTime.Time;
            return _lastSendTime + MinSendIntervalMs <= time;
        }

        private void ReceiveState(RigidBody2DState state)
        {
            if (_history.Count < 2)
            {
                ApplyState(state);
                return;
            }

            if (state.Timestamp < _history.Keys[0])
            {
                return;
            }
            
            SnapshotInterpolation.Sample(_history, state.Timestamp, out int from, out int to, out double t);
            var fromState = _history.Values[from];
            var toState = _history.Values[to];
            var interpolated = fromState.Interpolate(toState, (float)t);
            if (HasChanged(state, interpolated))
            {
                var time = NetworkTime.Time;
                var latestRecorded = _history.Values[_history.Count - 1];
                var correction = CorrectHistory(state, fromState, toState, to);
                
                long correctedTime = Math.Max(state.Timestamp, _lastRecordTime);
                long subIntervalMs = time - correctedTime;
                
                // Correct the subinterval (because we don't record every frame)
                if (subIntervalMs > 0)
                {
                    float dt = subIntervalMs * 0.001f; // To seconds

                    if (state.Timestamp > latestRecorded.Timestamp)
                    {
                        // Already at timestamp
                        correction.Position += correction.Velocity * dt;
                        correction.Rotation += correction.AngularVelocity * dt;
                    }
                    else
                    {
                        Vector2 unrecordedMove = Rb.position - latestRecorded.Position;
                        Vector2 velocityError = correction.Velocity - latestRecorded.Velocity;
                        correction.Position += unrecordedMove + (velocityError * dt);
                        correction.Rotation += (Rb.rotation - latestRecorded.Rotation) 
                                               + ((correction.AngularVelocity - latestRecorded.AngularVelocity) * dt);
                    }
                }
                
                ApplyState(correction);
            }
        }

        private RigidBody2DState CorrectHistory(RigidBody2DState corrected, RigidBody2DState from, RigidBody2DState to, int toIndex)
        {
            long prevDt = to.Timestamp - from.Timestamp;
            long correctDt = to.Timestamp - corrected.Timestamp;

            // Corrected is newer
            if (correctDt <= 0)
            {
                long clampedTime = Math.Min(corrected.Timestamp, NetworkTime.Time);
                corrected.Timestamp = clampedTime;
                corrected.PositionDelta = corrected.Position - to.Position;
                corrected.RotationDelta = corrected.Rotation - to.Rotation;
                corrected.VelocityDelta = corrected.Velocity - to.Velocity;
                corrected.AngularVelocityDelta = corrected.AngularVelocity - to.AngularVelocity;
                _lastRecordTime = clampedTime;
                _history[clampedTime] = corrected;
                if (_history.Count > HistoryLimit)
                {
                    _history.RemoveAt(0);
                }
                return corrected;
            }

            float multiplier = prevDt != 0 ? Mathf.Clamp01((float)correctDt / prevDt) : 0;
            var currentCorrection = corrected;
            var prevUncorrectedState = from;
            for (int i = toIndex; i < _history.Count; i++)
            {
                long time = _history.Keys[i];
                var uncorrectedState = _history.Values[i];
                var state = uncorrectedState;

                bool isAtTo = i == toIndex;
                float stepScale = isAtTo ? multiplier : 1f;
                // Need the correct time diff
                long stepMs = isAtTo ? correctDt : (time - prevUncorrectedState.Timestamp);
                float stepDt = Mathf.Max(0f, stepMs * 0.001f); // Dt in secs

                Vector2 velDelta = currentCorrection.Velocity - prevUncorrectedState.Velocity;
                float angVelDelta = currentCorrection.AngularVelocity - prevUncorrectedState.AngularVelocity;
                
                Vector2 posStep = (state.PositionDelta * stepScale) + (velDelta * stepDt);
                float rotStep = (state.RotationDelta * stepScale) + (angVelDelta * stepDt);
                Vector2 velStep = state.VelocityDelta * stepScale;
                float angVelStep = state.AngularVelocityDelta * stepScale;

                state.Position = currentCorrection.Position + posStep;
                state.Rotation = currentCorrection.Rotation + rotStep;
                state.Velocity = currentCorrection.Velocity + velStep;
                state.AngularVelocity = currentCorrection.AngularVelocity + angVelStep;
                
                // Compute relative to history predecessor in order to not keep scaling deltas multiple times
                if (isAtTo)
                {
                    float fullDt = Mathf.Max(0f, prevDt * 0.001f);
                    state.PositionDelta += velDelta * fullDt;
                    state.RotationDelta += angVelDelta * fullDt;
                }
                else
                {
                    state.PositionDelta = posStep;
                    state.RotationDelta = rotStep;
                    state.VelocityDelta = velStep;
                    state.AngularVelocityDelta = angVelStep;
                }

                prevUncorrectedState = uncorrectedState;
                _history[time] = state;
                currentCorrection = state;
            }
            return currentCorrection;
        }

        private void ApplyState(RigidBody2DState state)
        {
            Rb.position = state.Position;
            Rb.rotation = state.Rotation;
            if (state.Velocity.sqrMagnitude <= RestThreshold * RestThreshold
                && Mathf.Abs(state.AngularVelocity) <= RestThreshold)
            {
                Rb.linearVelocity = Vector2.zero;
                Rb.angularVelocity = 0;
            }
            else
            {
                Rb.linearVelocity = state.Velocity;
                Rb.angularVelocity = state.AngularVelocity;
            }
        }

        private void RecordState()
        {
            var time = NetworkTime.Time;
            _lastRecordTime = time;
            var state = BuildState(time);
            _history[state.Timestamp] = state;

            if (_history.Count > HistoryLimit)
            {
                _history.RemoveAt(0);
            }
        }

        private void SendUpdate(RigidBody2DState state)
        {
            _lastSentState = state;

            if (Reliable)
            {
                Rpc(RpcReceiveUpdateReliable, state);
            }
            else
            {
                Rpc(RpcReceiveUpdateUnreliable, state);
            }
        }

        [SosoRpc(RPC_CALL_TYPE.Client, false, false, SOSO_SEND_TYPE.Unreliable)]
        private void RpcReceiveUpdateUnreliable(RigidBody2DState state)
        {
            OnRpcReceiveState(state);
        }

        [SosoRpc(RPC_CALL_TYPE.Client, false, false, SOSO_SEND_TYPE.Reliable)]
        private void RpcReceiveUpdateReliable(RigidBody2DState state)
        {
            OnRpcReceiveState(state);
        }

        private void OnRpcReceiveState(RigidBody2DState state)
        {
            if (IsAuthority())
            {
                return;
            }
            long time = NetworkTime.ToLocalTime(NetId.AuthorityId, state.Timestamp);
            state.Timestamp = time;

            _receivedStates.EnqueueBack(state);
        }

        private bool HasChanged(RigidBody2DState state, RigidBody2DState lastState)
        {
            return SyncHelpers.HasChanged(state.Position, lastState.Position, PositionSensitivity) ||
                   SyncHelpers.HasChanged(state.Rotation, lastState.Rotation, RotationSensitivity) ||
                   SyncHelpers.HasChanged(state.Velocity, lastState.Velocity, LinearVelocitySensitivity) ||
                   SyncHelpers.HasChanged(state.AngularVelocity, lastState.AngularVelocity, AngularVelocitySensitivity);
        }

        private RigidBody2DState BuildState(long timestamp)
        {
            var position = Rb.position;
            var rotation = Rb.rotation;
            var linearVel = Rb.linearVelocity;
            var angularVel = Rb.angularVelocity;
            var positionDelta = Vector2.zero;
            var rotationDelta = 0f;
            var linearVelocityDelta = Vector2.zero;
            var angularVelocityDelta = 0f;

            if (_history.Count > 0)
            {
                var lastState = _history.Values[_history.Count - 1];
                positionDelta = position - lastState.Position;
                rotationDelta = rotation - lastState.Rotation;
                linearVelocityDelta = linearVel - lastState.Velocity;
                angularVelocityDelta = angularVel - lastState.AngularVelocity;
            }
            
            return new RigidBody2DState(
                timestamp,
                position,
                positionDelta,
                rotation,
                rotationDelta,
                linearVel,
                linearVelocityDelta,
                angularVel,
                angularVelocityDelta
            );
        }

        #region Authority Transfer
        
        private void OnTriggerEnter2D(Collider2D other) 
            => HandleBodyEntered(other.attachedRigidbody);
        private void OnTriggerExit2D(Collider2D other)
            => HandleBodyExited(other.attachedRigidbody);
        private void OnCollisionEnter2D(Collision2D collision)
            => HandleBodyEntered(collision.rigidbody);
        private void OnCollisionExit2D(Collision2D collision) 
            => HandleBodyExited(collision.rigidbody);

        private void HandleBodyEntered(Rigidbody2D otherRb)
        {
            if (AllowAuthoritySwap == false) return;

            if (TryGetBody(otherRb, out var rb) == false || rb.NetId.IsAuthority() == false)
            {
                return;
            }

            _localContactCount++;
            if (_pendingAuthGiveUp)
            {
                _pendingAuthGiveUp = false;
            }
            
            // Only request on initial contact
            if (_localContactCount == 1)
            {
                Rpc(RpcRequestOwnership, rb.NetId.AuthorityId);
            }
        }

        private void HandleBodyExited(Rigidbody2D otherRb)
        {
            if (AllowAuthoritySwap == false) return;

            if (TryGetBody(otherRb, out var rb) == false || rb.NetId.IsAuthority() == false)
            {
                return;
            }

            _localContactCount = Math.Max(0, _localContactCount - 1);

            if (_localContactCount == 0)
            {
                _contactExitTime = NetworkTime.Time;
                _pendingGiveUpOwnerId = rb.NetId.AuthorityId;
                _pendingAuthGiveUp = true;
            }
        }

        public override void OnAuthorityTransferred(ushort prevAuthority, ushort newAuthority)
        {
            base.OnAuthorityTransferred(prevAuthority, newAuthority);
            
            _receivedStates.Clear();
            _latestProcessedReceiveState = 0;
        }

        [SosoRpc(RPC_CALL_TYPE.Server, false, true)]
        private void RpcRequestGiveUpOwnership(ushort ownerId, RigidBody2DState state)
        {
            if (AllowAuthoritySwap == false) return;

            _collisions.Remove(ownerId);
            
            // Always apply outgoing authority's state
            if (NetId.AuthorityId == ownerId && IsAuthority() == false)
            {
                long time = NetworkTime.ToLocalTime(ownerId, state.Timestamp);
                state.Timestamp = time;

                // Immediately apply state
                ReceiveState(state);
            }
            
            ServerEvaluateAuthority();
        }

        [SosoRpc(RPC_CALL_TYPE.Server, false, true)]
        private void RpcRequestOwnership(ushort ownerId)
        {
            if (AllowAuthoritySwap == false) return;

            _collisions[ownerId] = NetworkTime.Time;
            ServerEvaluateAuthority();
        }

        private void ServerEvaluateAuthority()
        {
            if (_collisions.Count == 1)
            {
                var nextOwner = _collisions.Keys.First();
                if (NetId.AuthorityId != nextOwner)
                {
                    NetId.SetOwner(nextOwner);
                }
            }
            else
            {
                NetId.SetOwner(0);
            }
        }

        private HashSet<ushort> _authRemoveCache = new HashSet<ushort>();

        private void PruneInactiveAuthorityCollisions(long time)
        {
            _authRemoveCache.Clear();
            
            foreach (var kvp in _collisions)
            {
                var owner = kvp.Key;
                var heartbeatTime = kvp.Value;
                var lastUpdateDiff = time - heartbeatTime;
                if (lastUpdateDiff >= AuthorityHeartbeatTimeoutMs)
                {
                    NetworkLogger.Debug("Dropping inactive collision {id} from {me}. Last update: {t}", owner, name, lastUpdateDiff);
                    _authRemoveCache.Add(owner);
                }
            }
            if (_authRemoveCache.Count == 0)
            {
                return;
            }

            foreach (var owner in _authRemoveCache)
            {
                _collisions.Remove(owner);
            }
            ServerEvaluateAuthority();
        }

        private bool TryGetBody(Rigidbody2D collision, out NetworkedRigidBody2D rb)
        {
            rb = null;
            
            if (collision == false) return false;
            
            return collision.TryGetComponent(out rb);
        }
        
        #endregion
    }
}