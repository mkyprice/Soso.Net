using System;

namespace Soso.Net.Models
{
    [Serializable]
    public class RemoteInfo
    {
        public ushort SessionId;
        /// <summary>
        /// Time on remote adjusted for local
        /// </summary>
        public long RemoteTime;
        public long LastPingTime;
        public long PingMs;

        public override string ToString()
        {
            return $"{nameof(RemoteInfo)}(SID:{SessionId} RT:{RemoteTime} TD:{PingMs})";
        }
        
        public long ToLocalTime(long remoteTime)
        {
            return remoteTime 
                   + PingMs 
                   // + info.PingSec
                   + NetworkTime.TimeAdjustmentMs;
        }


        private long[] _pingAvgMs = new long[16];
        private int _pingIndex = 0;
        
        public void AddPingTime(long ping)
        {
            _pingAvgMs[_pingIndex] = ping;
            _pingIndex = (_pingIndex + 1) % _pingAvgMs.Length;
            PingMs = CalculatePing();
        }

        private long CalculatePing()
        {
            if (_pingAvgMs == null)
            {
                return 0;
            }
            long sum = 0;
            int totalCounted = 0;
            foreach (var time in _pingAvgMs)
            {
                if (time == 0) break;
                sum += time;
                totalCounted++;
            }

            if (totalCounted == 0) return 0;
            
            long pingMs = sum / totalCounted;
            return pingMs;
        }
    }
}