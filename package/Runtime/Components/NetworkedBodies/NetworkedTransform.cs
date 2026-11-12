using System;
using System.Collections.Generic;
using Soso.Net.Behaviors;
using Soso.Net.Behaviors.Rpc;
using Soso.Net.Components.NetworkedBodies.Helpers;
using Soso.Net.Components.NetworkedBodies.Packets;
using Soso.Net.Components.NetworkedBodies.Snapshots;
using Soso.Net.Logging;
using Soso.Net.Models;
using UnityEngine;

namespace Soso.Net.Components.NetworkedBodies
{
    public class NetworkedTransform : INetworkReceiver, INetworkPoolable
    {
        [SerializeField] public Transform Target;

        [SerializeField] public bool OnlySyncOnChange = true;
        [SerializeField] public bool Reliable;

        [SerializeField, Range(0.0001f, 1f)] public float PositionSensitivity = 0.05f;
        [SerializeField, Range(0.0001f, 1f)] public float RotationSensitivity = 0.05f;
        [SerializeField, Range(0.0001f, 1f)] public float ScaleSensitivity = 0.05f;
    
        [Header("Sync")]
        [SerializeField] public SYNC_METHOD SyncMethod = SYNC_METHOD.Update;
        [SerializeField] public bool SyncPosition = true;
        [SerializeField] public bool SyncRotation = true;
        [SerializeField] public bool SyncScale = true;
    
        [SerializeField, Range(0, 100)] public long SyncIntervalMs = 33;
    
        [Header("Interpolation")]
        [SerializeField] public bool InterpolatePosition = true;
        [SerializeField] public bool InterpolateRotation = true;
        [SerializeField] public bool InterpolateScale = true;

        [Tooltip("Offset for remote time")]
        [SerializeField] public int SnapshotBufferLimit = 64;
    
        public bool IsOwned => NetId == null || NetId.IsAuthority();
    
        public const int SNAPSHOT_CAPACITY = 16;
        protected readonly SortedList<long, TransformSnapshot> ClientSnapshots = new SortedList<long, TransformSnapshot>(SNAPSHOT_CAPACITY);
    
        protected TransformSnapshot LastSentSnapshot;
        protected TransformSnapshot? PendingSnapshot;
        private long _lastSendTime = 0;
        private bool _hasSyncedRestState = false;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
        }
#endif

        protected virtual void Awake()
        {
            if (Target == null)
            {
                Target = transform;
            }
        }

        protected override void Initialize()
        {
            ResetState();
        }


        protected virtual void FixedUpdate()
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

        protected virtual void LateUpdate()
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
                    SnapshotInterpolation.StepInterpolation(
                        ClientSnapshots, 
                        NetworkTime.Time, 
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

            SetPosition(position);
            SetRotation(rotation);

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
    
        [SosoRpc(RPC_CALL_TYPE.Client, false, false, SOSO_SEND_TYPE.Reliable)]
        private void RpcSnapshotReliable(SyncTransform sync)
        {
            OnSnapshotReceived(sync);
        }
        [SosoRpc(RPC_CALL_TYPE.Client, false, false, SOSO_SEND_TYPE.Unreliable)]
        private void RpcSnapshotUnreliable(SyncTransform sync)
        {
            OnSnapshotReceived(sync);
        }

        private void OnSnapshotReceived(SyncTransform sync)
        {
            var position = (sync.Modified & SyncTransform.MODIFIED.Position) != 0 ? sync.Position : (Vector3?)null;
            var rotation = (sync.Modified & SyncTransform.MODIFIED.Rotation) != 0 ? sync.Rotation : (Quaternion?)null;
            var scale = (sync.Modified & SyncTransform.MODIFIED.Scale) != 0 ? sync.Scale : (Vector3?)null;
            
            long snapshotTime = NetworkTime.ToLocalTime(NetId.AuthorityId, sync.Time);
            if (OnlySyncOnChange && NeedsCorrection(ClientSnapshots, snapshotTime))
            {
                RewriteHistory(ClientSnapshots, snapshotTime, GetPosition(), GetRotation(), GetScale());
            }
            
            AddSnapshot(ClientSnapshots, snapshotTime + SyncIntervalMs, position, rotation, scale);
        }
    
        [SosoRpc(RPC_CALL_TYPE.Client, false, false, SOSO_SEND_TYPE.Reliable)]
        private void RpcTeleport(SyncTransform sync)
        {
            if ((sync.Modified & SyncTransform.MODIFIED.Position) != 0)
            {
                SetPosition(sync.Position);
            }

            if ((sync.Modified & SyncTransform.MODIFIED.Rotation) != 0)
            {
                SetRotation(sync.Rotation);
            }

            if ((sync.Modified & SyncTransform.MODIFIED.Scale) != 0)
            {
                SetScale(sync.Scale);
            }
        
            ResetState();
        }

        protected SyncTransform GetSync(TransformSnapshot snapshot, TransformSnapshot previous, bool force)
        {
            bool positionChanged = SyncPosition && (force || SyncHelpers.HasChanged(previous.Position, snapshot.Position, PositionSensitivity));
            bool rotationChanged = SyncRotation && (force || SyncHelpers.HasChanged(previous.Rotation, snapshot.Rotation, RotationSensitivity));
            bool scaleChanged    = SyncScale && (force || SyncHelpers.HasChanged(previous.Scale, snapshot.Scale, ScaleSensitivity));
        
            return new SyncTransform()
            {
                Modified = (positionChanged ? SyncTransform.MODIFIED.Position : SyncTransform.MODIFIED.None) |
                           (rotationChanged ? SyncTransform.MODIFIED.Rotation : SyncTransform.MODIFIED.None) |
                           (scaleChanged ? SyncTransform.MODIFIED.Scale : SyncTransform.MODIFIED.None),
                Time = NetworkTime.Time,
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
            long localTime = NetworkTime.Time;
            return localTime - _lastSendTime >= SyncIntervalMs;
        }
        
        private void ReadyToSync()
        {
            _lastSendTime = NetworkTime.Time;
            var snapshot = BuildSnapshot();
            bool hasChanged = OnlySyncOnChange == false || HasChanged(LastSentSnapshot, snapshot);
            bool forceSync = _hasSyncedRestState == false;
            if (hasChanged || forceSync)
            {
                _hasSyncedRestState = hasChanged == false;

                var sync = GetSync(snapshot, LastSentSnapshot, forceSync);
                LastSentSnapshot  = snapshot;

                if (Reliable)
                {
                    Rpc(RpcSnapshotReliable, sync);
                }
                else
                {
                    Rpc(RpcSnapshotUnreliable, sync);
                }
            }
            OnReadyToSync(forceSync);
        }

        protected virtual void OnReadyToSync(bool force)
        {
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

        protected void AddSnapshot(SortedList<long, TransformSnapshot> snapshots, long time, Vector3? position, Quaternion? rotation, Vector3? scale)
        {
            if (position.HasValue == false) position = snapshots.Count > 0 ? snapshots.Values[snapshots.Count - 1].Position : GetPosition();
            if (rotation.HasValue == false) rotation = snapshots.Count > 0 ? snapshots.Values[snapshots.Count - 1].Rotation : GetRotation();
            if (scale.HasValue == false)    scale    = snapshots.Count > 0 ? snapshots.Values[snapshots.Count - 1].Scale : GetScale();

            while (snapshots.Count > SnapshotBufferLimit)
            {
                snapshots.RemoveAt(0);
            }
            
            var snapshot = new TransformSnapshot(time, position.Value, rotation.Value, scale.Value);
            snapshots[snapshot.Timestamp] = snapshot;
        }

        private void RewriteHistory(SortedList<long, TransformSnapshot> snapshots, long time, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            snapshots.Clear();
            
            TransformSnapshot snapshot = new TransformSnapshot(time - SyncIntervalMs, pos, rot, scale);
            snapshots[snapshot.Timestamp] = snapshot;
        }

        private bool NeedsCorrection(SortedList<long, TransformSnapshot> snapshots, long incomingTime)
        {
            return snapshots.Count == 1 && incomingTime - snapshots.Keys[0] >= SyncIntervalMs * 2;// + NetworkTime.TimeAdjustmentMs;
        }

        protected virtual void Apply(TransformSnapshot interpolated, TransformSnapshot goal)
        {
            if (SyncPosition) SetPosition(InterpolatePosition ? interpolated.Position : goal.Position);
            if (SyncRotation) SetRotation(InterpolateRotation ? interpolated.Rotation : goal.Rotation);
            if (SyncScale)    SetScale(InterpolateScale ? interpolated.Scale : goal.Scale);
        }

        protected virtual void SetPosition(Vector3 position)
        {
            Target.position = position;
        }

        protected virtual void SetRotation(Quaternion rotation)
        {
            Target.rotation = rotation;
        }

        protected virtual void SetScale(Vector3 scale)
        {
            Target.localScale = scale;
        }

        protected virtual Vector3 GetPosition()
        {
            return Target.position;
        }

        protected virtual Quaternion GetRotation()
        {
            return Target.rotation;
        }

        protected virtual Vector3 GetScale()
        {
            return Target.localScale;
        }

        protected virtual TransformSnapshot BuildSnapshot()
        {
            return new TransformSnapshot(NetworkTime.Time, GetPosition(), GetRotation(), GetScale());
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
