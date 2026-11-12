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
        public Vector2 LinearVelocity;
        public float Rotation;
        public float AngularVelocity;

        public RigidBody2DState(long timestamp, Vector2 position, float rotation, Vector2 linearVelocity, float angularVelocity)
        {
            Timestamp = timestamp;
            Position = position;
            Rotation = rotation;
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
        }

        public RigidBody2DState Interpolate(RigidBody2DState other, float t)
        {
            return new RigidBody2DState(
                NetMath.Lerp(Timestamp, other.Timestamp, t),
                Vector2.Lerp(Position, other.Position, t),
                Mathf.LerpAngle(Rotation, other.Rotation, t),
                Vector2.Lerp(LinearVelocity, other.LinearVelocity, t),
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
                reader.Read(out value.LinearVelocity);
                reader.Read(out value.AngularVelocity);
                return value;
            }

            public override void Serialize(ref ByteWriter writer, RigidBody2DState value, SerializationConfig config)
            {
                writer.Write(value.Timestamp);
                writer.Write(value.Position);
                writer.Write(value.Rotation);
                writer.Write(value.LinearVelocity);
                writer.Write(value.AngularVelocity);
            }
        }
    }
}