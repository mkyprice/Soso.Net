using System.Runtime.InteropServices;
using Soso.Net.Serializers;
using Soso.Serialization;
using Soso.Serialization.Binary;
using UnityEngine;

namespace Soso.Net.Components.PredictedBodies
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RigidBody2DState
    {
        public double Timestamp;
        public Vector2 Position;
        public Quaternion Rotation;
        public Vector2 Velocity;
        public float AngularVelocity;

        public RigidBody2DState(double timestamp, Vector2 position, Quaternion rotation, Vector2 velocity, float angularVelocity)
        {
            Timestamp = timestamp;
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            AngularVelocity = angularVelocity;
        }

        public class Serializer : SosoSerializer<RigidBody2DState>
        {
            public override RigidBody2DState Deserialize(ref ByteReader reader, SerializationConfig config)
            {
                return reader.ReadBlittable<RigidBody2DState>();
            }

            public override void Serialize(ref ByteWriter writer, RigidBody2DState value, SerializationConfig config)
            {
                writer.WriteBlittable(value);
            }
        }
    }
}