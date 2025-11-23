using System;
using System.Diagnostics;

namespace Brudixy
{
    public struct StopwatchSlim
    {
        public static readonly Stopwatch m_timer = new ();

        static StopwatchSlim()
        {
            m_timer.Start();
        }

        private readonly TimeSpan m_startTime;

        private StopwatchSlim(bool slim) => m_startTime = m_timer.Elapsed;

        public static StopwatchSlim StartNew() => new StopwatchSlim(true);

        public TimeSpan Elapsed => m_timer.Elapsed - m_startTime;

        public long ElapsedMilliseconds => (long)Elapsed.TotalMilliseconds;
    }
}