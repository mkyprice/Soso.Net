using System.Collections.Generic;
using Soso.Net.Extensions;
using Soso.Net.Utils;
using UnityEngine;

namespace Soso.Net.Components.NetworkedBodies.Snapshots
{
    public static class SnapshotInterpolation
    {
        public static void Sample<T>(SortedList<long, T> snapshots, long localTime, out int from, out int to, out double time)
            where T : ISnapshot
        {
            to = from = -1;
            time = 0;

            for (int i = 0; i < snapshots.Count - 1; i++)
            {
                var a = snapshots.Values[i];
                var b = snapshots.Values[i + 1];

                // PPLogger.Debug(CHANNEL.Network, "LocalTime: {t} RemoteTimeA: {a} RemoteTimeB: {b}", localTime, a.RemoteTime, b.RemoteTime);
                if (localTime >= a.Timestamp && localTime <= b.Timestamp)
                {
                    from = i;
                    to = i + 1;
                    time = NetMath.InverseLerp(a.Timestamp, b.Timestamp, localTime);
                    return;
                }
            }

            if (snapshots.Values[0].Timestamp > localTime)
            {
                to = from = 0;
                time = 0;
            }
            else
            {
                to = from = snapshots.Count - 1;
                time = 0;
            }
        }
        
        public static void StepInterpolation<T>(SortedList<long, T> snapshots, long localTime, out T fromSnapshot, out T toSnapshot, out double time)
            where T : ISnapshot
        {
            Sample(snapshots, localTime, out int from, out int to, out time);
            
            fromSnapshot = snapshots.Values[from];
            toSnapshot = snapshots.Values[to];

            snapshots.RemoveCount(from); //+ 1);

            // PPLogger.Error(CHANNEL.Network, "Removed {count} of {total} snapshots", from + 1, snapshots.Count);
        }
    }
}