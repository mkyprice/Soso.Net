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
using UnityEngine;

namespace Soso.Net.Components.PredictedBodies
{
    public class PredictedRigidBody2D : INetworkReceiver
    {
        [SerializeField, Range(0.0001f, 1f)] public float PositionSensitivity = 0.05f;
        [SerializeField, Range(0.0001f, 1f)] public float RotationSensitivity = 0.05f;
        [SerializeField, Range(0.0001f, 1f)] public float LinearVelocitySensitivity = 0.05f;
        [SerializeField, Range(0.0001f, 1f)] public float AngularVelocitySensitivity = 0.05f;
        [SerializeField, Range(0.0001f, 10f)] public float SnapThreshold = 2f;
        [SerializeField, Min(2)] public int HistoryLimit = 32;
        [SerializeField, Range(20, 200)] public long RecordingIntervalMs = 50;
        [SerializeField] public bool OnlySyncOnChange = false;
        [SerializeField, Range(20, 5000)] public long SendIntervalMs = 500;
        [SerializeField, Range(20, 500)] public long MinSendIntervalMs = 100;
        [SerializeField] public bool AllowAuthoritySwap = false;
        [SerializeField] public bool Reliable;
        
        public Rigidbody2D Rb;

        private Dictionary<ushort, long> _collisions = new Dictionary<ushort, long>();
        private SortedList<long, RigidBody2DState> _history = new SortedList<long, RigidBody2DState>();
        private RigidBody2DState _lastSentState;
        private long _lastSendTime;
        private long _lastRecordTime;

        private void Awake()
        {
            if (Rb == false)
            {
                Rb = GetComponent<Rigidbody2D>();
            }
        }

        private void FixedUpdate()
        {
            if (IsRecordReady() || _history.Count < 2)
            {
                RecordState();
            }
        }

        private void LateUpdate()
        {
            if (NetId == null) return;
            if (NetId.IsAuthority())
            {
                var time = NetworkTime.Time;
                var state = BuildState(time);
                if ((OnlySyncOnChange == false && IsSendReady()) || (IsMinSendReady() && HasChanged(state, _lastSentState)))
                {
                    _lastSendTime = time;
                    SendUpdate(state);
                }
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
                    Vector2 unrecordedMove = Rb.position - latestRecorded.Position;
                    Vector2 velocityError = correction.Velocity - latestRecorded.Velocity;
                    correction.Position += unrecordedMove + (velocityError * dt);
                    correction.Rotation += (Rb.rotation - latestRecorded.Rotation) 
                                           + ((correction.AngularVelocity - latestRecorded.AngularVelocity) * dt);
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
                _history[to.Timestamp] = corrected;
                return corrected;
            }

            float multiplier = prevDt != 0 ? Mathf.Clamp01((float)correctDt / prevDt) : 0;
            to.PositionDelta = Vector2.Lerp(Vector2.zero, to.PositionDelta, multiplier);
            to.RotationDelta = Mathf.Lerp(0, to.RotationDelta, multiplier);
            to.VelocityDelta = Vector2.Lerp(Vector2.zero, to.VelocityDelta, multiplier);
            to.AngularVelocityDelta = Mathf.Lerp(0, to.AngularVelocityDelta, multiplier);
            _history[to.Timestamp] = to;

            var currentCorrection = corrected;
            for (int i = toIndex; i < _history.Count; i++)
            {
                long time = _history.Keys[i];
                var state = _history.Values[i];

                state.Position = currentCorrection.Position + state.PositionDelta;
                state.Rotation = currentCorrection.Rotation + state.RotationDelta;
                state.Velocity = currentCorrection.Velocity + state.VelocityDelta;
                state.AngularVelocity = currentCorrection.AngularVelocity + state.AngularVelocityDelta;

                _history[time] = state;
                currentCorrection = state;
            }
            return currentCorrection;
        }

        private void ApplyState(RigidBody2DState state)
        {
            Rb.position = state.Position;
            Rb.rotation = state.Rotation;
            Rb.linearVelocity = state.Velocity;
            Rb.angularVelocity = state.AngularVelocity;
            
            // Immediately snap to the state since we're hardly moving
            if (Rb.linearVelocity.magnitude <= SnapThreshold && Mathf.Abs(Rb.angularVelocity) <= SnapThreshold)
            {
                _history.Clear();
                _history.Add(state.Timestamp, BuildState(state.Timestamp));
            }
            Physics2D.SyncTransforms();
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

            ReceiveState(state);
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (AllowAuthoritySwap == false) return;
            
            if (TryGetBody(other.attachedRigidbody, out var rb) == false || rb.NetId.IsAuthority() == false)
            {
                return;
            }

            Rpc(RpcRequestOwnership, rb.NetId.AuthorityId);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (AllowAuthoritySwap == false) return;

            if (TryGetBody(other.attachedRigidbody, out var rb) == false || rb.NetId.IsAuthority() == false)
            {
                return;
            }
            
            Rpc(RpcRequestGiveUpOwnership, rb.NetId.AuthorityId, BuildState(NetworkTime.Time));
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (AllowAuthoritySwap == false) return;

            if (TryGetBody(collision.rigidbody, out var rb) == false || rb.NetId.IsAuthority() == false)
            {
                return;
            }

            Rpc(RpcRequestOwnership, rb.NetId.AuthorityId);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (AllowAuthoritySwap == false) return;

            if (TryGetBody(collision.rigidbody, out var rb) == false || rb.NetId.IsAuthority() == false)
            {
                return;
            }
            
            Rpc(RpcRequestGiveUpOwnership, rb.NetId.AuthorityId, BuildState(NetworkTime.Time));
        }

        [SosoRpc(RPC_CALL_TYPE.Server, false, true)]
        private void RpcRequestGiveUpOwnership(ushort ownerId, RigidBody2DState state)
        {
            if (AllowAuthoritySwap == false) return;

            _collisions.Remove(ownerId);
            if (NetId.AuthorityId == ownerId)
            {
                if (_collisions.Count <= 0 || _collisions.Count > 1)
                {
                    long time = NetworkTime.ToLocalTime(ownerId, state.Timestamp);
                    state.Timestamp = time;
                    ReceiveState(state);
                    
                    NetId.SetOwner(0);
                }
                else
                {
                    NetId.SetOwner(_collisions.Keys.First());
                }
            }
        }

        [SosoRpc(RPC_CALL_TYPE.Server, false, true)]
        private void RpcRequestOwnership(ushort ownerId)
        {
            if (AllowAuthoritySwap == false) return;

            _collisions[ownerId] = NetworkTime.Time;
            if (_collisions.Count == 1)
            {
                NetId.SetOwner(ownerId);
            }
            else
            {
                NetId.SetOwner(0);
            }
        }

        public override void OnAuthorityTransferred(ushort prevAuthority, ushort newAuthority)
        {
            var sId = INetworkManager.SessionId;
            
            // Record auth transfer time
            if (sId == prevAuthority || sId == newAuthority)
            {
            }
        }

        private bool TryGetBody(Rigidbody2D collision, out NetworkedRigidBody2D rb)
        {
            rb = null;
            
            if (collision == false) return false;
            
            return collision.TryGetComponent(out rb);
        }
    }
}