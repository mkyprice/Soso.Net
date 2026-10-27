using UnityEngine;

namespace Soso.Net.Components.NetworkedBodies.Snapshots
{
    public struct TransformSnapshot : ISnapshot
    {
        public double Timestamp { get; set; }
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public TransformSnapshot(double remoteTime, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Timestamp = remoteTime;
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public static TransformSnapshot Interpolate(TransformSnapshot a, TransformSnapshot b, float t, bool interpolatePosition, bool interpolateRotation, bool interpolateScale)
        {
            var snapshot = new TransformSnapshot(0,
                interpolatePosition ? Vector3.LerpUnclamped(a.Position, b.Position, t) : b.Position,
                 interpolateRotation ? Quaternion.SlerpUnclamped(a.Rotation, b.Rotation, t) : b.Rotation,
                interpolateScale ? Vector3.LerpUnclamped(a.Scale, b.Scale, t) : b.Scale
                );
            return snapshot;
        }
    }
}