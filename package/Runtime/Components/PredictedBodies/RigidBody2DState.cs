using System;
using Soso.Net.Components.NetworkedBodies.Snapshots;
using Soso.Net.Extensions;
using Soso.Net.Serializers;
using Soso.Net.Utils;
using Soso.Serialization;
using Soso.Serialization.Binary;
using UnityEngine;

namespace Soso.Net.Components.PredictedBodies
{
    public struct RigidBody2DState : ISnapshot
    {
        public long Timestamp { get; set; }
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public float AngularVelocity;
        
        [NonSerialized] public Vector2 PositionDelta;
        [NonSerialized] public Vector2 VelocityDelta;
        [NonSerialized] public float RotationDelta;
        [NonSerialized] public float AngularVelocityDelta;

        public RigidBody2DState(long timestamp, Vector2 position, float rotation, Vector2 velocity, float angularVelocity)
            : this(timestamp, position, Vector2.zero, rotation, 0, velocity, Vector2.zero, angularVelocity, 0)
        {
        }

        public RigidBody2DState(long timestamp, Vector2 position, Vector2 positionDelta, float rotation, float rotationDelta, Vector2 velocity, Vector2 velocityDelta, float angularVelocity, float angularVelocityDelta)
        {
            Timestamp = timestamp;
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            AngularVelocity = angularVelocity;
            PositionDelta = positionDelta;
            RotationDelta = rotationDelta;
            VelocityDelta = velocityDelta;
            AngularVelocityDelta = angularVelocityDelta;
        }

        public RigidBody2DState Interpolate(RigidBody2DState other, float t)
        {
            return new RigidBody2DState(
                NetMath.Lerp(Timestamp, other.Timestamp, t),
                Vector2.Lerp(Position, other.Position, t),
                Mathf.Lerp(Rotation, other.Rotation, t),
                Vector2.Lerp(Velocity, other.Velocity, t),
                Mathf.Lerp(AngularVelocity, other.AngularVelocity, t)
            );
        }

        public class Serializer : SosoSerializer<RigidBody2DState>
        {
            public override RigidBody2DState Deserialize(ref ByteReader reader, SerializationConfig config)
            {
                var value = new RigidBody2DState();
                value.Timestamp = reader.ReadLong();
                reader.Read(out value.Position);
                reader.Read(out value.Rotation);
                reader.Read(out value.Velocity);
                reader.Read(out value.AngularVelocity);
                return value;
            }

            public override void Serialize(ref ByteWriter writer, RigidBody2DState value, SerializationConfig config)
            {
                writer.Write(value.Timestamp);
                writer.Write(value.Position);
                writer.Write(value.Rotation);
                writer.Write(value.Velocity);
                writer.Write(value.AngularVelocity);
            }
        }
    }
}