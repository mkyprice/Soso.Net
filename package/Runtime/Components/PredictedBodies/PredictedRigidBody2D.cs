using System;
using System.Collections.Generic;
using Soso.Net.Behaviors;
using Soso.Net.Behaviors.Rpc;
using Soso.Net.Components.NetworkedBodies;
using Soso.Net.Components.NetworkedBodies.Helpers;
using Soso.Net.Components.NetworkedBodies.Snapshots;
using Soso.Net.Logging;
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
        
        public Rigidbody2D Rb;
        
        private Transform _transform;

        [SerializeField, Min(1)] public int HistoryLimit = 32;
        [SerializeField] public long RecordingIntervalMs = 5;
        private SortedList<long, RigidBody2DState> _history = new SortedList<long, RigidBody2DState>();
        private RigidBody2DState _lastState;

        private void Awake()
        {
            if (Rb == false)
            {
                Rb = GetComponent<Rigidbody2D>();
            }
            _transform = Rb.transform;
        }

        private void FixedUpdate()
        {
            RecordState();
        }

        private void LateUpdate()
        {
            if (NetId == null) return;
            if (NetId.IsAuthority())
            {
                var state = BuildState(NetworkTime.Time);
                if (HasChanged(state, _lastState))
                {
                    SendUpdate(state);
                }
            }
        }

        private void ReceiveState(RigidBody2DState state)
        {
            RigidBody2DState currentState;
            if (_history.Count > 0)
            {
                currentState = _history.Values[_history.Count - 1];
            }
            else
            {
                currentState = BuildState(NetworkTime.Time);
            }
            if (HasChanged(state, currentState) == false)
            {
                return;
            }
            
            RecordState();

            if (_history.Count < 2)
            {
                return;
            }

            var oldestState = _history.Values[0];
            var newestState = _history.Values[_history.Count - 1];

            if (state.Timestamp < oldestState.Timestamp)
            {
                ApplyState(state);
                return;
            }

            if (newestState.Timestamp < oldestState.Timestamp)
            {
                ApplyState(state);
                return;
            }
            
            SnapshotInterpolation.Sample(_history, state.Timestamp, out int from, out int to, out double t);
            var fromState = _history.Values[from];
            var toState = _history.Values[to];
            var interpolated = fromState.Interpolate(toState, (float)t);
            if (HasChanged(interpolated, currentState))
            {
                var correction = CorrectHistory(state, fromState, toState, to);
                ApplyState(correction);
            }
        }

        private RigidBody2DState CorrectHistory(RigidBody2DState corrected, RigidBody2DState from, RigidBody2DState to, int toIndex)
        {
            if (_history.Count >= HistoryLimit)
            {
                _history.RemoveAt(0);
                toIndex--;
            }

            long prevDt = to.Timestamp - from.Timestamp;
            long correctDt = to.Timestamp - corrected.Timestamp;

            float multiplier = prevDt != 0 ? (float)correctDt / prevDt : 0;
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
            if (Rb.linearVelocity.magnitude <= SnapThreshold && Rb.angularVelocity <= SnapThreshold)
            {
                _history.Clear();
                _history.Add(state.Timestamp, BuildState(state.Timestamp));
            }
            Physics2D.SyncTransforms();
        }

        private void RecordState()
        {
            var state = BuildState(NetworkTime.Time);
            _lastState = state;
            _history[state.Timestamp] = state;

            if (_history.Count > HistoryLimit)
            {
                _history.RemoveAt(0);
            }
        }

        private void SendUpdate(RigidBody2DState state)
        {
            _lastState = state;

            Rpc(RpcReceiveUpdate, state);
        }

        [SosoRpc(RPC_CALL_TYPE.Client, false, false)]
        private void RpcReceiveUpdate(RigidBody2DState state)
        {
            long time = NetworkTime.ToLocalTime(NetId.AuthorityId, state.Timestamp);
            state.Timestamp = time + RecordingIntervalMs;

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

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (NetId.IsAuthority())
            {
                return;
            }
            
            if (TryGetBody(collision, out var rb) == false || rb.NetId.IsAuthority() == false)
            {
                return;
            }
            
            NetId.TakeOwnership();
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (NetId.IsAuthority() == false) return;
            
            if (TryGetBody(collision, out var rb) == false || rb.NetId.IsAuthority() == false)
            {
                return;
            }
            
            NetId.GiveUpOwnership();
        }

        public override void OnAuthorityTransferred(ushort prevAuthority, ushort newAuthority)
        {
            var sId = INetworkManager.SessionId;
            if (sId == prevAuthority || sId == newAuthority)
            {
                
            }
        }

        private bool TryGetBody(Collision2D collision, out NetworkedRigidBody2D rb)
        {
            rb = null;
            
            if (collision.rigidbody == false) return false;
            
            return collision.rigidbody.TryGetComponent(out rb);
        }
    }
}