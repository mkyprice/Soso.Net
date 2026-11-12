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
        [SerializeField, Min(0)] public float TeleportThreshold = 2f;
        [SerializeField, Range(0, 1f)] public float RestThreshold = 0.1f;
        [SerializeField, Range(0.5f, 1f)] public float MoveCoefficient = 0.5f;
        [SerializeField, Min(2)] public int HistoryLimit = 32;
        [SerializeField, Range(0, 200)] public long RecordingIntervalMs = 20;
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
        private RigidBody2DState? _pendingHandoffState = null;
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

        public override void OnSpawn()
        {
            base.OnSpawn();
            
            ResetInstance();
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            
            ResetInstance();
        }

        private void LateUpdate()
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
        }

        private void FixedUpdate()
        {
            if (IsInitialized == false)
            {
                return;
            }
            var time = NetworkTime.Time;
            bool isAuthority = NetId.IsAuthority();
            
            if (IsRecordReady() || _history.Count < 2)
            {
                RecordState(time);
            }

            if (_pendingHandoffState.HasValue)
            {
                ReceiveState(_pendingHandoffState.Value, time);
                _pendingHandoffState = null;
            }
            else if (isAuthority == false)
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
                    ReceiveState(latestState.Value, time);
                }
            }

            if (_pendingAuthGiveUp && time >= _contactExitTime + AuthorityDropDelayMs)
            {
                _pendingAuthGiveUp = false;
                Rpc(RpcRequestGiveUpOwnership, _pendingGiveUpOwnerId, BuildState(time));
            }
            if (isAuthority && _localContactCount > 0 && time >= _lastAuthHeartbeatTime + AuthorityHeartbeatMs)
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

        private void ResetInstance()
        {
            _pendingAuthGiveUp = false;
            _lastSendTime = 0;
            _lastRecordTime = 0;
            _localContactCount = 0;
            _latestProcessedReceiveState = 0;
            _pendingHandoffState = null;
            _receivedStates.Clear();
            _history.Clear();
            _collisions.Clear();
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

        private void ReceiveState(RigidBody2DState state, long time)
        {
            if (state.LinearVelocity.sqrMagnitude <= RestThreshold * RestThreshold
                && Mathf.Abs(state.AngularVelocity) <= RestThreshold)
            {
                state.LinearVelocity = Vector2.zero;
                state.AngularVelocity = 0f;
            }

            if (_history.Count < 2)
            {
                ApplyState(state);
                ResetHistoryToState(state, time);
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

            // We are groovy, no need for any corrections
            if (HasChanged(state, interpolated) == false)
            {
                return;
            }

            if (SyncHelpers.HasChanged(state.Position, interpolated.Position, TeleportThreshold))
            {
                // Something has gone wrong, and we are far from where we should be
                // Teleport and clear history
                ApplyState(state);
                ResetHistoryToState(state, time);
                NetworkLogger.Debug("Teleported {name}", name);
                return;
            }

            // Unrecorded physics movement between the last recorded history and now
            var latestRecorded = _history.Values[_history.Count - 1];
            Vector2 unrecordedMove = Rb.position - latestRecorded.Position;
            float unrecordedRot = Mathf.DeltaAngle(latestRecorded.Rotation, Rb.rotation);

            // Correct recorded history from to up to _lastRecordTime
            var correction = CorrectHistory(state, interpolated);
            if (state.Timestamp < _lastRecordTime)
            {
                correction.Position += unrecordedMove;
                correction.Rotation += unrecordedRot;
            }

            ApplyState(correction);

            // Apply the correction to history
            long lastKey = _history.Keys[_history.Count - 1];
            correction.Timestamp = lastKey;
            _history[lastKey] = correction;
        }

        private RigidBody2DState CorrectHistory(RigidBody2DState authorityState, RigidBody2DState predictedAtAuthTime)
        {
            bool isAtRest = authorityState.LinearVelocity == Vector2.zero && authorityState.AngularVelocity == 0f;
            float moveScale = isAtRest ? 1f : MoveCoefficient;

            // Compute the error at time
            Vector2 posError = (authorityState.Position - predictedAtAuthTime.Position) * moveScale;
            float rotError = Mathf.DeltaAngle(predictedAtAuthTime.Rotation, authorityState.Rotation) * moveScale;
            Vector2 velError = (authorityState.LinearVelocity - predictedAtAuthTime.LinearVelocity) * moveScale;
            float angVelError = (authorityState.AngularVelocity - predictedAtAuthTime.AngularVelocity) * moveScale;

            // Shift history by error
            RigidBody2DState latest = authorityState;
            for (int i = 0; i < _history.Count; i++)
            {
                long key = _history.Keys[i];
                var s = _history.Values[i];
                s.Position += posError;
                s.Rotation += rotError;
                s.LinearVelocity += velError;
                s.AngularVelocity += angVelError;
                _history[key] = s;
                latest = s;
            }

            if (authorityState.LinearVelocity == Vector2.zero && authorityState.AngularVelocity == 0f)
            {
                latest.LinearVelocity = Vector2.zero;
                latest.AngularVelocity = 0f;
            }

            return latest;
        }

        private void ResetHistoryToState(RigidBody2DState state, long time)
        {
            _history.Clear();
            if (state.Timestamp < time)
            {
                _history[state.Timestamp] = state;
            }
            RecordState(time);
        }

        private void ApplyState(RigidBody2DState state)
        {
            Rb.position = state.Position;
            Rb.rotation = state.Rotation;
            Rb.linearVelocity = state.LinearVelocity;
            Rb.angularVelocity = state.AngularVelocity;
        }

        private void RecordState(long time)
        {
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
            _receivedStates.EnqueueBack(state);
        }

        private bool HasChanged(RigidBody2DState state, RigidBody2DState lastState)
        {
            return SyncHelpers.HasChanged(state.Position, lastState.Position, PositionSensitivity) ||
                   SyncHelpers.HasChanged(state.Rotation, lastState.Rotation, RotationSensitivity) ||
                   SyncHelpers.HasChanged(state.LinearVelocity, lastState.LinearVelocity, LinearVelocitySensitivity) ||
                   SyncHelpers.HasChanged(state.AngularVelocity, lastState.AngularVelocity, AngularVelocitySensitivity);
        }

        private RigidBody2DState BuildState(long timestamp)
        {
            var position = Rb.position;
            var rotation = Rb.rotation;
            var linearVel = Rb.linearVelocity;
            var angularVel = Rb.angularVelocity;
            
            return new RigidBody2DState(
                timestamp,
                position,
                rotation,
                linearVel,
                angularVel
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
                _pendingHandoffState = state;
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