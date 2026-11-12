using System;
using Soso.Net.Extensions;
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
        public long Time;
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
                    reader.Read(out value.Position);
                }

                if ((value.Modified & MODIFIED.Rotation) > 0)
                {
                    reader.Read(out value.Rotation);
                }

                if ((value.Modified & MODIFIED.Scale) > 0)
                {
                    reader.Read(out value.Scale);
                }
                return value;
            }

            public void Serialize(ref ByteWriter writer, SyncTransform value, SerializationConfig config)
            {
                writer.Write((byte)value.Modified);

                writer.Write(value.Time);
                
                if ((value.Modified & MODIFIED.Position) > 0)
                {
                    writer.Write(value.Position);
                }

                if ((value.Modified & MODIFIED.Rotation) > 0)
                {
                    writer.Write(value.Rotation);
                }

                if ((value.Modified & MODIFIED.Scale) > 0)
                {
                    writer.Write(value.Scale);
                }
            }
        }
    }
}