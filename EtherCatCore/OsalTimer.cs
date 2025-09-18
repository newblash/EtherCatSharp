using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace EtherCatSharp.EtherCatCore
{
    public class OsalTimer
    {
        private long _startTimeTicks;
        private long _timeoutMicroseconds;
        private readonly double _ticksPerMicrosecond;

        public OsalTimer()
        {
            var frequency = Stopwatch.Frequency;
            _ticksPerMicrosecond = (double)frequency / 1_000_000.0;
        }

        public void Start(long timeoutMicroseconds)
        {
            _startTimeTicks = Stopwatch.GetTimestamp();
            _timeoutMicroseconds = timeoutMicroseconds;
        }

        public bool IsExpired()
        {
            long elapsedTicks = Stopwatch.GetTimestamp() - _startTimeTicks;
            double elapsedMicroseconds = elapsedTicks / _ticksPerMicrosecond;

            return elapsedMicroseconds >= _timeoutMicroseconds;
        }
        public int osal_usleep(uint usec)
        {
            Start(usec);
            if (usec >= 1000)
            {
            }
            while (!IsExpired()) ;
            return 1;
        }
    }

}
