using Soso.Net.Behaviors.Rpc;
using Soso.Net.Components.NetworkedBodies.Helpers;
using Soso.Net.Components.NetworkedBodies.Packets;
using Soso.Net.Components.NetworkedBodies.Snapshots;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using UnityEngine;
using CHANNEL = Soso.Net.Logging.NetworkLogger.CHANNEL;

namespace Soso.Net.Components.NetworkedBodies
{
    public class NetworkedRigidBody2D : NetworkedTransform
    {
        [Header("RigidBody2D")]
        [SerializeField] public bool SyncLinearVelocity = true;
        [SerializeField] public bool SyncAngularVelocity = true;
        
        [SerializeField] public float LinearVelocitySensitivity = 0.1f;
        [SerializeField] public float AngularVelocitySensitivity = 0.1f;

        public Rigidbody2D Rb;
        private SyncRigidBody2D _lastSentSync;

        protected override void Awake()
        {
            base.Awake();
            
            if (Target.gameObject.TryGetComponent(out Rb) == false)
            {
                NetworkLogger.Error(CHANNEL.Default, "No RigidBody attached to {name}", Target.gameObject.name);
            }
        }

        protected override void ReadyToSync()
        {
            base.ReadyToSync();
            
            SyncRigidBody2D rbSync = BuildSync();
            if (HasChanged(rbSync, _lastSentSync))
            {
                _lastSentSync = rbSync;

                Rpc(RpcSyncRb, rbSync);
            }
        }

        private bool HasChanged(SyncRigidBody2D from, SyncRigidBody2D to)
        {
            bool lvChanged = SyncLinearVelocity && (SyncHelpers.HasChanged(from.LinearVelocity, to.LinearVelocity, LinearVelocitySensitivity));
            bool avChanged = SyncAngularVelocity && (SyncHelpers.HasChanged(from.AngularVelocity, to.AngularVelocity, AngularVelocitySensitivity));
            
            return lvChanged || avChanged;
        }

        protected SyncRigidBody2D BuildSync()
        {
            SyncRigidBody2D sync = new SyncRigidBody2D()
            {
                Modified = SyncRigidBody2D.MODIFIED.LinearVelocity | SyncRigidBody2D.MODIFIED.AngularVelocity,
                LinearVelocity = Rb.linearVelocity,
                AngularVelocity = Rb.angularVelocity,
            };
            return sync;
        }

        protected override void Apply(TransformSnapshot interpolated, TransformSnapshot goal)
        {
            base.Apply(interpolated, goal);
            
            // Rb.SyncTransform();
        }

        [SosoRpc(RPC_CALL_TYPE.Client, false, false)]
        private void RpcSyncRb(SyncRigidBody2D sync)
        {
            Rb.linearVelocity = sync.LinearVelocity;
            Rb.angularVelocity = sync.AngularVelocity;
        }

        public override void ResetState()
        {
            base.ResetState();
            _lastSentSync = default;
            // Rb.linearVelocity = Vector2.zero;
            // Rb.angularVelocity = 0;
        }
    }
}