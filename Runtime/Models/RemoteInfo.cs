namespace Soso.Net.Models
{
    public class RemoteInfo
    {
        public ushort SessionId;
        /// <summary>
        /// Time on remote adjusted for local
        /// </summary>
        public double RemoteTime;
        /// <summary>
        /// Difference between remote and local time
        /// </summary>
        public double TimeDifference;
        public double LastPingTime;

        public override string ToString()
        {
            return $"{nameof(RemoteInfo)}(SID:{SessionId} RT:{RemoteTime} TD:{TimeDifference})";
        }

        public double Ping
        {
            get
            {
                double sum = 0;
                for (int i = 0; i < _pingAvg.Length; i++)
                {
                    sum += _pingAvg[i];
                }
                return sum / _pingAvg.Length;
            }
            set
            {
                AddPing(value);
            }
        }

        private double[] _pingAvg = new double[16];
        private int _pingIndex = 0;
        
        public void AddPing(double ping)
        {
            _pingAvg[_pingIndex] = ping;
            _pingIndex = (_pingIndex + 1) % _pingAvg.Length;
        }
    }
}