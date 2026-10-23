using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StartupProfiler
{
    // Everything recorded during the start-up, in milliseconds since the profiler started
    public static class Timeline
    {
        public class LogLine
        {
            public double time;
            public string source;
            public LogLevel level;
            public string text;
        }

        public class MarkEntry
        {
            public double time;
            public string name;
        }

        // the main-thread time spent in one piece of code, e.g. a content pack provider, and when it first and last ran
        public class Timing
        {
            public string category;
            public string name;
            public string assembly;
            public long ticks;
            public int calls;
            public double firstStart = -1;
            public double lastEnd;

            public double Milliseconds => ticks * 1000.0 / Stopwatch.Frequency;

            public void Add(double start, long elapsedTicks)
            {
                ticks += elapsedTicks;
                calls++;
                if (firstStart < 0)
                {
                    firstStart = start;
                }
                lastEnd = Math.Max(lastEnd, start + elapsedTicks * 1000.0 / Stopwatch.Frequency);
            }
        }

        private class Listener : ILogListener
        {
            public void LogEvent(object sender, LogEventArgs eventArgs)
            {
                var line = new LogLine
                {
                    time = Now,
                    source = eventArgs.Source?.SourceName ?? "",
                    level = eventArgs.Level,
                    text = eventArgs.Data?.ToString() ?? "",
                };
                lock (Lines)
                {
                    Lines.Add(line);
                }
                BootstrapPatches.OnLogLine(line);
            }

            public void Dispose()
            {
            }
        }

        private static readonly Stopwatch clock = new Stopwatch();
        public static readonly List<LogLine> Lines = new List<LogLine>();
        public static readonly List<MarkEntry> Marks = new List<MarkEntry>();
        private static readonly Dictionary<(string category, string name), Timing> timings = new Dictionary<(string category, string name), Timing>();
        // how long the process ran before the preloader, i.e. Unity's own start-up before any mod
        public static double BeforePreloader { get; private set; }
        public static ManualLogSource Log { get; private set; }

        public static double Now => clock.Elapsed.TotalMilliseconds;

        public static void Start()
        {
            clock.Start();
            try
            {
                BeforePreloader = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
            }
            catch (Exception)
            {
                BeforePreloader = -1;
            }
            Log = Logger.CreateLogSource("StartupProfiler");
            Logger.Listeners.Add(new Listener());
            Mark("Profiler started, before the patchers");
        }

        public static void Mark(string name)
        {
            lock (Marks)
            {
                Marks.Add(new MarkEntry { time = Now, name = name });
            }
        }

        public static Timing GetTiming(string category, string name, string assembly)
        {
            lock (timings)
            {
                if (!timings.TryGetValue((category, name), out var timing))
                {
                    timing = new Timing { category = category, name = name, assembly = assembly };
                    timings[(category, name)] = timing;
                }
                return timing;
            }
        }

        public static List<Timing> GetTimings(string category)
        {
            lock (timings)
            {
                var result = new List<Timing>();
                foreach (var timing in timings.Values)
                {
                    if (timing.category == category)
                    {
                        result.Add(timing);
                    }
                }
                return result;
            }
        }

        // the last line logged before the given time, to tell what was going on then
        public static LogLine LastLineBefore(double time)
        {
            lock (Lines)
            {
                for (int i = Lines.Count - 1; i >= 0; i--)
                {
                    if (Lines[i].time <= time)
                    {
                        return Lines[i];
                    }
                }
                return null;
            }
        }
    }
}
