using Soso.Net.Behaviors.Rpc;
using Soso.Net.Components.NetworkedBodies.Helpers;
using Soso.Net.Components.NetworkedBodies.Packets;
using Soso.Net.Components.NetworkedBodies.Snapshots;
using Soso.Net.Logging;
using Soso.Net.Models;
using UnityEngine;

namespace Soso.Net.Components.NetworkedBodies
{
    public class NetworkedRigidBody2D : NetworkedTransform
    {
        [Header("RigidBody2D")]
        [SerializeField] public bool SyncLinearVelocity = true;
        [SerializeField] public bool SyncAngularVelocity = true;
        
        [SerializeField, Range(0.0001f, 1f)] public float LinearVelocitySensitivity = 0.1f;
        [SerializeField, Range(0.0001f, 1f)] public float AngularVelocitySensitivity = 0.1f;
        
        [SerializeField, Range(0.00001f, 0.1f)] public float RestThreshold = 0.01f;

        public Rigidbody2D Rb;

        private SyncRigidBody2D _lastSentSync;
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SyncMethod = SYNC_METHOD.FixedUpdate;
        }
#endif

        protected override void Awake()
        {
            base.Awake();

            SyncMethod = SYNC_METHOD.FixedUpdate;
            
            if (Target.gameObject.TryGetComponent(out Rb) == false)
            {
                NetworkLogger.Error("No RigidBody attached to {name}", Target.gameObject.name);
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (NetId.IsAuthority() == false)
            {
                Rb.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                Rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            
            if (NetId != null && NetId.IsAuthority() == false)
            {
                Rb.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                Rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }

        protected override void OnReadyToSync(bool force)
        {
            base.OnReadyToSync(force);
            SyncRigidBody2D rbSync = BuildSync(_lastSentSync, force);
            if (OnlySyncOnChange == false || rbSync.Modified != SyncRigidBody2D.MODIFIED.None)
            {
                _lastSentSync = rbSync;

                if (Reliable)
                {
                    Rpc(RpcSyncRbReliable, rbSync);
                }
                else
                {
                    Rpc(RpcSyncRbUnreliable, rbSync);
                }
            }
        }

        protected SyncRigidBody2D BuildSync(SyncRigidBody2D previous, bool forceUpdate)
        {
            var lv = Rb.linearVelocity;
            if (lv.sqrMagnitude <= RestThreshold * RestThreshold)
            {
                lv = Vector2.zero;
            }
            var av = Rb.angularVelocity;
            if (Mathf.Abs(av) <= RestThreshold)
            {
                av = 0;
            }
            bool lvStopped = lv == Vector2.zero && previous.LinearVelocity != Vector2.zero;
            bool avStopped = av == 0 && previous.AngularVelocity != 0;
            
            bool lvChanged = SyncLinearVelocity && (lvStopped || forceUpdate || SyncHelpers.HasChanged(previous.LinearVelocity, lv, LinearVelocitySensitivity));
            bool avChanged = SyncAngularVelocity && (avStopped || forceUpdate || SyncHelpers.HasChanged(previous.AngularVelocity, av, AngularVelocitySensitivity));
            
            SyncRigidBody2D sync = new SyncRigidBody2D()
            {
                Modified = (lvChanged ? SyncRigidBody2D.MODIFIED.LinearVelocity : SyncRigidBody2D.MODIFIED.None) |
                            (avChanged ? SyncRigidBody2D.MODIFIED.AngularVelocity : SyncRigidBody2D.MODIFIED.None),
                LinearVelocity = lv,
                AngularVelocity = av,
            };
            return sync;
        }

        protected override void Apply(TransformSnapshot interpolated, TransformSnapshot goal)
        {
            base.Apply(interpolated, goal);
            
            // If we are out of snapshots, we are not moving
            if (ClientSnapshots.Count <= 1)
            {
                if (SyncPosition) Rb.linearVelocity = Vector2.zero;
                if (SyncRotation) Rb.angularVelocity = 0;
            }
        }

        protected override void SetPosition(Vector3 position)
        {
            Rb.position = position;
        }

        protected override void SetRotation(Quaternion rotation)
        {
            Rb.rotation = rotation.eulerAngles.z;
        }

        protected override Vector3 GetPosition()
        {
            return Rb.position;
        }

        protected override Quaternion GetRotation()
        {
            return Quaternion.Euler(0, 0, Rb.rotation);
        }

        [SosoRpc(RPC_CALL_TYPE.Client, false, false, SOSO_SEND_TYPE.Reliable)]
        private void RpcSyncRbReliable(SyncRigidBody2D sync)
        {
            ApplySync(sync);
        }

        [SosoRpc(RPC_CALL_TYPE.Client, false, false, SOSO_SEND_TYPE.Unreliable)]
        private void RpcSyncRbUnreliable(SyncRigidBody2D sync)
        {
            ApplySync(sync);
        }

        private void ApplySync(SyncRigidBody2D sync)
        {
            if ((sync.Modified & SyncRigidBody2D.MODIFIED.LinearVelocity) != 0)
            {
                Rb.linearVelocity = sync.LinearVelocity;
            }
            
            if ((sync.Modified & SyncRigidBody2D.MODIFIED.AngularVelocity) != 0)
            {
                Rb.angularVelocity = sync.AngularVelocity;
            }
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