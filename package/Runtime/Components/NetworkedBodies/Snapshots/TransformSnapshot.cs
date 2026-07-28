using UnityEngine;

namespace Soso.Net.Components.NetworkedBodies.Snapshots
{
    public struct TransformSnapshot : ISnapshot
    {
        /// <summary>
        /// When it was sent
        /// </summary>
        public double RemoteTime { get; set; }
        /// <summary>
        /// When it was received
        /// </summary>
        public double LocalTime { get; set; }
        
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public TransformSnapshot(double remoteTime, double localTime, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            RemoteTime = remoteTime;
            LocalTime = localTime;
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public static TransformSnapshot Interpolate(TransformSnapshot a, TransformSnapshot b, float t, bool interpolatePosition, bool interpolateRotation, bool interpolateScale)
        {
            var snapshot = new TransformSnapshot(0, 0, 
                interpolatePosition ? Vector3.LerpUnclamped(a.Position, b.Position, t) : b.Position,
                 interpolateRotation ? Quaternion.SlerpUnclamped(a.Rotation, b.Rotation, t) : b.Rotation,
                interpolateScale ? Vector3.LerpUnclamped(a.Scale, b.Scale, t) : b.Scale
                );
            return snapshot;
        }
    }
}