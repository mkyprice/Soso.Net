using System;
using UnityEngine;

namespace Soso.Net.Components.NetworkedBodies.Helpers
{
    public static class SyncHelpers
    {
        public static bool HasChanged(Quaternion a, Quaternion b, float precision)
        {
            return Quaternion.Angle(a, b) > precision;
        }
    
        public static bool HasChanged(Vector3 a, Vector3 b, float precision)
        {
            if (Math.Sign(a.x) != Math.Sign(b.x) || Math.Sign(a.y) != Math.Sign(b.y) || Math.Sign(a.z) != Math.Sign(b.z)) return true;
            float aMag = a.sqrMagnitude;
            float bMag = b.sqrMagnitude;
            float diff = Mathf.Sqrt(Mathf.Abs(aMag - bMag));
            return diff > precision;
        }

        public static bool HasChanged(float a, float b, float precision)
        {
            float diff = Mathf.Sqrt(Mathf.Abs(a - b));
            return diff > precision;
        }
    }
}