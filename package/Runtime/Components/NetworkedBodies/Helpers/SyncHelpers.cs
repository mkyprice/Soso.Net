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
            return (a - b).sqrMagnitude > precision * precision;
        }

        public static bool HasChanged(float a, float b, float precision)
        {
            return Mathf.Abs(a - b) > precision;
        }
    }
}