using System;
using Soso.Serialization;
using Soso.Serialization.Binary;
using Soso.Serialization.Serializers;
using UnityEngine;

namespace Soso.Net.Components.NetworkedBodies.Packets
{
    public struct SyncTransform
    {
        [Flags]
        public enum MODIFIED : byte
        {
            None = 0,
            Position = 1 << 0,
            Rotation = 1 << 1,
            Scale = 1 << 2,
        }
        
        public MODIFIED Modified;
        public double Time;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public override string ToString()
        {
            return $"{nameof(SyncTransform)}:{Modified}";
        }

        public class Serializer : ISerializer<SyncTransform>
        {
            public SyncTransform Deserialize(ref ByteReader reader, SerializationConfig config)
            {
                SyncTransform value = new SyncTransform();
                value.Modified = (MODIFIED)reader.ReadByte();
                
                reader.Read(out value.Time);
                
                if ((value.Modified & MODIFIED.Position) > 0)
                {
                    value.Position = new Vector3(
                        reader.ReadFloat(), 
                        reader.ReadFloat(), 
                        reader.ReadFloat()
                        );
                }

                if ((value.Modified & MODIFIED.Rotation) > 0)
                {
                    value.Rotation = new Quaternion(
                        reader.ReadFloat(), 
                        reader.ReadFloat(), 
                        reader.ReadFloat(), 
                        reader.ReadFloat()
                        );
                }

                if ((value.Modified & MODIFIED.Scale) > 0)
                {
                    value.Scale = new Vector3(
                        reader.ReadFloat(), 
                        reader.ReadFloat(), 
                        reader.ReadFloat()
                        );
                }
                return value;
            }

            public void Serialize(ref ByteWriter writer, SyncTransform value, SerializationConfig config)
            {
                writer.Write((byte)value.Modified);

                writer.Write(value.Time);
                
                if ((value.Modified & MODIFIED.Position) > 0)
                {
                    writer.Write(value.Position.x);
                    writer.Write(value.Position.y);
                    writer.Write(value.Position.z);
                }

                if ((value.Modified & MODIFIED.Rotation) > 0)
                {
                    writer.Write(value.Rotation.x);
                    writer.Write(value.Rotation.y);
                    writer.Write(value.Rotation.z);
                    writer.Write(value.Rotation.w);
                }

                if ((value.Modified & MODIFIED.Scale) > 0)
                {
                    writer.Write(value.Scale.x);
                    writer.Write(value.Scale.y);
                    writer.Write(value.Scale.z);
                }
            }
            
            public void Serialize(ref ByteWriter writer, object value, SerializationConfig config)
            {
                Serialize(ref writer, (SyncTransform)value, config);
            }

            object ISerializer.Deserialize(ref ByteReader reader, SerializationConfig config)
            {
                return Deserialize(ref reader, config);
            }
        }
    }
}