namespace Miruken.Infrastructure
{
    using System;

    public static class DateTime2
    {
        private static int m_offset;
        static DateTime2()
        {
            var s = DateTime.Now.Second;
            while (true)
            {
                var s2 = DateTime.Now.Second;
                // wait for a rollover
                if (s != s2)
                {
                    m_offset = Environment.TickCount % 1000;
                    break;
                }
            }
        }

        public static DateTime Now
        {
            get
            {
                // find where we are based on the os tick
                var tick = Environment.TickCount % 1000;

                // calculate our ms shift from our base m_offset
                var ms = (tick >= m_offset) ? (tick - m_offset) : (1000 - (m_offset - tick));

                // build a new DateTime with our calculated ms
                // we use a new DateTime because some devices fill ms with a non-zero garbage value
                var now = DateTime.Now;
                return new DateTime(now.Year, now.Month, now.Day, now.Hour,now.Month, now.Second, ms);
            }
        }
    }
}
