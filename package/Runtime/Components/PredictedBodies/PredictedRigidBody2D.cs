using System.Collections.Generic;
using Soso.Net.Behaviors;
using Soso.Net.Behaviors.Rpc;
using Soso.Net.Components.NetworkedBodies.Helpers;
using UnityEngine;

namespace Soso.Net.Components.PredictedBodies
{
    public class PredictedRigidBody2D : INetworkReceiver
    {
        [SerializeField] public double StateRecordTime = 0.1;
        
        [SerializeField, Range(0.0001f, 1f)] public float PositionSensitivity = 0.05f;
        [SerializeField, Range(0.0001f, 1f)] public float RotationSensitivity = 0.05f;
        [SerializeField, Range(0.0001f, 1f)] public float LinearVelocitySensitivity = 0.05f;
        [SerializeField, Range(0.0001f, 1f)] public float AngularVelocitySensitivity = 0.05f;
        
        public Rigidbody2D Rb;
        
        private Transform _transform;

        private SortedList<double, RigidBody2DState> _myStates = new SortedList<double, RigidBody2DState>();
        private Queue<RigidBody2DState> _receivedStates = new Queue<RigidBody2DState>();
        
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
            if (NetId.IsOwner)
            {
                var state = GetCurrentState();
                if (HasChanged(state, _lastState))
                {
                    SendUpdate(state);
                }
            }
            else
            {
                CheckHistory();
            }
        }

        private void CheckHistory()
        {
            while (_receivedStates.TryDequeue(out var receivedState))
            {
                RigidBody2DState stateToApply = receivedState;
                foreach (var kvp in _myStates)
                {
                    var time = kvp.Key;
                    var state = kvp.Value;
                    if (time >= receivedState.Timestamp + NetId.Ping)
                    {
                        if (HasChanged(receivedState, state))
                        {
                            stateToApply = state;
                        }
                        break;
                    }
                }

                ApplyState(stateToApply);
            }
        }

        private void ApplyState(RigidBody2DState state)
        {
            Rb.position = state.Position;
            Rb.rotation = state.Rotation;
            Rb.linearVelocity = state.Velocity;
            Rb.angularVelocity = state.AngularVelocity;

            Physics2D.SyncTransforms();
        }

        private void RecordState()
        {
            var state = BuildState();
            _myStates[state.Timestamp] = state;
            bool removed = false;
            do
            {
                if (_myStates.Keys[0] < state.Timestamp - StateRecordTime)
                {
                    _myStates.RemoveAt(0);
                    removed = true;
                }
                else
                {
                    removed = false;
                }
            } while (removed);
        }

        private RigidBody2DState GetCurrentState()
        {
            if (_myStates.Count <= 0)
            {
                return BuildState();
            }
            return _myStates.Values[^1];
        }

        private void SendUpdate(RigidBody2DState state)
        {
            _lastState = state;

            Rpc(RpcReceiveUpdate, state);
        }

        [SosoRpc(RPC_CALL_TYPE.Client, false, false)]
        private void RpcReceiveUpdate(RigidBody2DState state)
        {
            _receivedStates.Enqueue(state);
        }

        private bool HasChanged(RigidBody2DState state, RigidBody2DState lastState)
        {
            return SyncHelpers.HasChanged(state.Position, lastState.Position, PositionSensitivity) ||
                   SyncHelpers.HasChanged(state.Rotation, lastState.Rotation, RotationSensitivity) ||
                   SyncHelpers.HasChanged(state.Velocity, lastState.Velocity, LinearVelocitySensitivity) ||
                   SyncHelpers.HasChanged(state.AngularVelocity, lastState.AngularVelocity, AngularVelocitySensitivity);
        }

        private RigidBody2DState BuildState()
        {
            return new RigidBody2DState(
                NetworkTime.LocalTime,
                Rb.position,
                Rb.rotation,
                Rb.linearVelocity,
                Rb.angularVelocity
            );
        }
    }
}