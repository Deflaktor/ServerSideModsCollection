using System.Collections;
using System.Diagnostics;

namespace StartupProfiler
{
    // Times each step of a coroutine. The coroutines it yields run as steps of their own, by Unity or the game's
    // initializer loop, so they are wrapped too and count towards the same timing.
    public class TimedEnumerator : IEnumerator
    {
        private readonly IEnumerator inner;
        private readonly Timeline.Timing timing;
        private object current;

        public TimedEnumerator(IEnumerator inner, Timeline.Timing timing)
        {
            this.inner = inner;
            this.timing = timing;
        }

        public object Current => current;

        public bool MoveNext()
        {
            double start = Timeline.Now;
            long startTicks = Stopwatch.GetTimestamp();
            bool hasNext;
            try
            {
                hasNext = inner.MoveNext();
            }
            finally
            {
                timing.Add(start, Stopwatch.GetTimestamp() - startTicks);
            }
            current = hasNext ? Wrap(inner.Current, timing) : null;
            return hasNext;
        }

        public void Reset()
        {
            inner.Reset();
        }

        public static object Wrap(object value, Timeline.Timing timing)
        {
            return value is IEnumerator enumerator && !(value is TimedEnumerator) ? new TimedEnumerator(enumerator, timing) : value;
        }
    }
}
