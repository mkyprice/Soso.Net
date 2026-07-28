using System;
using Soso.Serialization;
using Soso.Serialization.Binary;
using Soso.Serialization.Serializers;
using UnityEngine;

namespace Soso.Net.Components.NetworkedBodies.Packets
{
    public struct SyncRigidBody2D
    {
        [Flags]
        public enum MODIFIED : byte
        {
            None = 0,
            LinearVelocity = 1 << 0,
            AngularVelocity = 1 << 1,
        }
        
        public MODIFIED Modified;
        public Vector2 LinearVelocity;
        public float AngularVelocity;

        public override string ToString()
        {
            return $"{nameof(SyncRigidBody2D)}:{Modified}";
        }

        public class Serializer : ISerializer<SyncRigidBody2D>
        {
            public SyncRigidBody2D Deserialize(ref ByteReader reader, SerializationConfig config)
            {
                SyncRigidBody2D value = new SyncRigidBody2D();
                value.Modified = (MODIFIED)reader.ReadByte();
                if ((value.Modified & MODIFIED.LinearVelocity) > 0)
                {
                    value.LinearVelocity = new Vector2(
                        reader.ReadFloat(), 
                        reader.ReadFloat()
                        );
                }

                if ((value.Modified & MODIFIED.AngularVelocity) > 0)
                {
                    value.AngularVelocity = reader.ReadFloat();
                }

                return value;
            }

            public void Serialize(ref ByteWriter writer, SyncRigidBody2D value, SerializationConfig config)
            {
                writer.Write((byte)value.Modified);
                
                if ((value.Modified & MODIFIED.LinearVelocity) > 0)
                {
                    writer.Write(value.LinearVelocity.x);
                    writer.Write(value.LinearVelocity.y);
                }

                if ((value.Modified & MODIFIED.AngularVelocity) > 0)
                {
                    writer.Write(value.AngularVelocity);
                }
            }
            
            public void Serialize(ref ByteWriter writer, object value, SerializationConfig config)
            {
                Serialize(ref writer, (SyncRigidBody2D)value, config);
            }

            object ISerializer.Deserialize(ref ByteReader reader, SerializationConfig config)
            {
                return Deserialize(ref reader, config);
            }
        }
    }
}