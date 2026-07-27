using System.Collections.Generic;
using Soso.Net.Behaviors;
using Soso.Net.Behaviors.Rpc;
using Soso.Net.Components.NetworkedBodies.Helpers;
using Soso.Net.Components.NetworkedBodies.Packets;
using Soso.Net.Components.NetworkedBodies.Snapshots;
using Soso.Net.Logging;
using Soso.Net.Models.Packets;
using UnityEngine;
using CHANNEL = Soso.Net.Logging.NetworkLogger.CHANNEL;

namespace Soso.Net.Components.NetworkedBodies
{
    public class NetworkedTransform : INetworkReceiver, INetworkPoolable
    {
        [SerializeField] public Transform Target;

        [SerializeField] public bool OnlySyncOnChange = true;

        [SerializeField, Range(0.0001f, 1f)] public float PositionSensitivity = 0.05f;
        [SerializeField, Range(0.0001f, 1f)] public float RotationSensitivity = 0.05f;
        [SerializeField, Range(0.0001f, 1f)] public float ScaleSensitivity = 0.05f;
    
        [Header("Sync")]
        [SerializeField] public SYNC_METHOD SyncMethod = SYNC_METHOD.Update;
        [SerializeField] public bool SyncPosition = true;
        [SerializeField] public bool SyncRotation = true;
        [SerializeField] public bool SyncScale = true;
    
        [SerializeField, Range(0, 10)] public float SyncInterval = 0.05f;
    
        [Header("Interpolation")]
        [SerializeField] public bool InterpolatePosition = true;
        [SerializeField] public bool InterpolateRotation = true;
        [SerializeField] public bool InterpolateScale = true;

        [Tooltip("Offset for remote time")]
        [SerializeField] public int SnapshotBufferLimit = 64;
    
        public bool IsOwned => NetId == null || NetId.IsOwner;
    
        public const int SNAPSHOT_CAPACITY = 16;
        protected readonly SortedList<double, TransformSnapshot> ClientSnapshots = new SortedList<double, TransformSnapshot>(SNAPSHOT_CAPACITY);
    
        protected TransformSnapshot LastSentSnapshot;
        protected TransformSnapshot? PendingSnapshot;
        private double _lastSendTime = 0;

        protected virtual void Awake()
        {
            if (Target == null)
            {
                Target = transform;
            }
        }

        protected override void Initialize()
        {
            if (INetworkManager.GetInstance().IsOffline)
            {
                NetworkLogger.Info(CHANNEL.Default, "You are offline. Disabling {name}", nameof(NetworkedTransform));
                enabled = false;
                return;
            }

            ResetState();
        }


        private void FixedUpdate()
        {
            if (SyncMethod == SYNC_METHOD.FixedUpdate)
            {
                PerformUpdate();
            }
            if (PendingSnapshot.HasValue && IsOwned == false && SyncMethod == SYNC_METHOD.FixedUpdate)
            {
                Apply(PendingSnapshot.Value, PendingSnapshot.Value);
                PendingSnapshot = null;
            }
        }

        private void Update()
        {
            if (SyncMethod == SYNC_METHOD.Update)
            {
                PerformUpdate();
            }
        }

        private void PerformUpdate()
        {
            if (IsOwned)
            {
                if (IsSendReady())
                {
                    ReadyToSync();
                }
            }
            else
            {
                if (ClientSnapshots.Count > 0)
                {
                    NetworkLogger.Debug(CHANNEL.Default, "Performing step interpolation on {count} snapshots", ClientSnapshots.Count);
                    
                    SnapshotInterpolation.StepInterpolation(
                        ClientSnapshots, 
                        NetworkTime.LocalTime, 
                        out var from, 
                        out var to, 
                        out var t
                    );
                    var computed = TransformSnapshot.Interpolate(from, to, (float)t, InterpolatePosition, InterpolateRotation, InterpolateScale);

                    if (SyncMethod == SYNC_METHOD.FixedUpdate)
                    {
                        PendingSnapshot = computed;
                    }
                    else
                    {
                        Apply(computed, to);
                    }
                    NetworkLogger.Debug(CHANNEL.Default, "Finished step interpolation with {count} remaining snapshots", ClientSnapshots.Count);
                }
            }
        }

        #region Network Functions

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (IsOwned == false)
            {
                return;
            }
            
            Target.position = position;
            Target.rotation = rotation;

            SyncTransform sync = new SyncTransform()
            {
                Modified = SyncTransform.MODIFIED.Position | SyncTransform.MODIFIED.Rotation,
                Position = position,
                Rotation = rotation,
            };
            Rpc(RpcTeleport, sync);
        }

        #endregion

        #region Network Callbacks
    
        [SosoRpc(RPC_CALL_TYPE.Client, false, false)]
        private void RpcSnapshot(SyncTransform sync)
        {
            var position = (sync.Modified & SyncTransform.MODIFIED.Position) != 0 ? sync.Position : (Vector3?)null;
            var rotation = (sync.Modified & SyncTransform.MODIFIED.Rotation) != 0 ? sync.Rotation : (Quaternion?)null;
            var scale = (sync.Modified & SyncTransform.MODIFIED.Scale) != 0 ? sync.Scale : (Vector3?)null;
            
            var localTime = NetworkTime.LocalTime;

            double snapshotTime = NetworkTime.ToLocalTime(NetId.OwnerId, sync.Time);
            if (OnlySyncOnChange && NeedsCorrection(ClientSnapshots, localTime, snapshotTime))
            {
                RewriteHistory(ClientSnapshots, snapshotTime, localTime, Target.position, Target.rotation, Target.localScale);
            }
            
            AddSnapshot(ClientSnapshots, snapshotTime + SyncInterval, position, rotation, scale);
        }
    
        [SosoRpc(RPC_CALL_TYPE.Client, false, false)]
        private void RpcTeleport(SyncTransform sync)
        {
            if ((sync.Modified & SyncTransform.MODIFIED.Position) != 0)
            {
                Target.position = sync.Position;
            }

            if ((sync.Modified & SyncTransform.MODIFIED.Rotation) != 0)
            {
                Target.rotation = sync.Rotation;
            }

            if ((sync.Modified & SyncTransform.MODIFIED.Scale) != 0)
            {
                Target.localScale = sync.Scale;
            }
        
            ResetState();
        }

        protected SyncTransform GetSync(TransformSnapshot snapshot, TransformSnapshot previous)
        {
            bool positionChanged = SyncPosition && (SyncHelpers.HasChanged(previous.Position, snapshot.Position, PositionSensitivity));
            bool rotationChanged = SyncRotation && (SyncHelpers.HasChanged(previous.Rotation, snapshot.Rotation, RotationSensitivity));
            bool scaleChanged    = SyncScale && (SyncHelpers.HasChanged(previous.Scale, snapshot.Scale, ScaleSensitivity));
        
            return new SyncTransform()
            {
                Modified = (positionChanged ? SyncTransform.MODIFIED.Position : SyncTransform.MODIFIED.None) |
                           (rotationChanged ? SyncTransform.MODIFIED.Rotation : SyncTransform.MODIFIED.None) |
                           (scaleChanged ? SyncTransform.MODIFIED.Scale : SyncTransform.MODIFIED.None),
                Time = NetworkTime.LocalTime,
                Position = snapshot.Position,
                Rotation = snapshot.Rotation,
                Scale = snapshot.Scale
            };
        }

        #endregion

        #region Helper Functions
        
        protected bool IsSendReady()
        {
            if (IsInitialized == false) return false;
            double localTime = NetworkTime.LocalTime;
            return localTime - _lastSendTime >= SyncInterval;
        }
        
        protected virtual void ReadyToSync()
        {
            var snapshot = BuildSnapshot();
            if (OnlySyncOnChange == false || HasChanged(LastSentSnapshot, snapshot))
            {
                _lastSendTime = NetworkTime.LocalTime;

                var sync = GetSync(snapshot, LastSentSnapshot);
                LastSentSnapshot  = snapshot;

                Rpc(RpcSnapshot, sync);
            }
        }
    
        public virtual void ResetState()
        {
            ClientSnapshots.Clear();
        }

        protected virtual bool HasChanged(TransformSnapshot previous, TransformSnapshot current)
        {
            bool positionChanged = SyncPosition && (SyncHelpers.HasChanged(previous.Position, current.Position, PositionSensitivity));
            bool rotationChanged = SyncRotation && (SyncHelpers.HasChanged(previous.Rotation, current.Rotation, RotationSensitivity));
            bool scaleChanged    = SyncScale && (SyncHelpers.HasChanged(previous.Scale, current.Scale, ScaleSensitivity));
        
            return positionChanged || rotationChanged || scaleChanged;
        }

        protected void AddSnapshot(SortedList<double, TransformSnapshot> snapshots, double time, Vector3? position, Quaternion? rotation, Vector3? scale)
        {
            if (position.HasValue == false) position = snapshots.Count > 0 ? snapshots.Values[snapshots.Count - 1].Position : Target.position;
            if (rotation.HasValue == false) rotation = snapshots.Count > 0 ? snapshots.Values[snapshots.Count - 1].Rotation : Target.rotation;
            if (scale.HasValue == false)    scale    = snapshots.Count > 0 ? snapshots.Values[snapshots.Count - 1].Scale : Target.localScale;

            if (snapshots.Count > SnapshotBufferLimit)
            {
                return;
            }
            
            var snapshot = new TransformSnapshot(time, NetworkTime.LocalTime, position.Value, rotation.Value, scale.Value);
            snapshots[snapshot.RemoteTime] = snapshot;
        }

        private void RewriteHistory(SortedList<double, TransformSnapshot> snapshots, double time, double localTime, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            snapshots.Clear();
            
            TransformSnapshot snapshot = new TransformSnapshot(time - SyncInterval, localTime - SyncInterval, pos, rot, scale);
            snapshots[snapshot.RemoteTime] = snapshot;
        }

        private bool NeedsCorrection(SortedList<double, TransformSnapshot> snapshots, double localTime, double time)
        {
            return snapshots.Count == 1 && time - snapshots.Keys[0] >= localTime;
        }


        protected virtual void Apply(TransformSnapshot interpolated, TransformSnapshot goal)
        {
            if (SyncPosition) Target.position = InterpolatePosition ? interpolated.Position : goal.Position;
            if (SyncRotation) Target.rotation = InterpolateRotation ? interpolated.Rotation : goal.Rotation;
            if (SyncScale)    Target.localScale = InterpolateScale ? interpolated.Scale : goal.Scale;
        }

        protected virtual TransformSnapshot BuildSnapshot()
        {
            return new TransformSnapshot(NetworkTime.LocalTime, 0, Target.position, Target.rotation, Target.localScale);
        }

        #endregion

        public override void OnSpawn()
        {
            base.OnSpawn();
            ResetState();
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            ResetState();
        }
    }
}
