using UnityEngine;

namespace Soso.Net.Utils
{
    public static class NetMath
    {
        public static long Lerp(long a, long b, float t)
        {
            t = Mathf.Clamp01(t);
            return (long)(a + (b - a) * t);
        }
        public static float InverseLerp(long a, long b, long value)
        {
            return Mathf.Clamp01(InverseLerpUnclamped(a, b, value));
        }
        public static float InverseLerpUnclamped(long a, long b, long value)
        {
            if (a == b) return 0;
            float t = (float)(((double)value - a) / ((double)b - a));
            return t;
        }
    }
}