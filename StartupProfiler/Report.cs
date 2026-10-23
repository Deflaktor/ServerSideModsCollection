using BepInEx;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace StartupProfiler
{
    // Sums up where the start-up's time went, into BepInEx/StartupProfile.txt, and the most telling parts into the log
    public static class Report
    {
        public const string StepSeparator = " / ";
        public const string FileName = "StartupProfile.txt";
        private const int TopCount = 40;
        public static bool Written { get; private set; }

        public static void Write(string reason)
        {
            if (Written)
            {
                return;
            }
            Written = true;
            try
            {
                var summary = new StringBuilder();
                var details = new StringBuilder();
                Build(reason, summary, details);
                var path = Path.Combine(Paths.BepInExRootPath, FileName);
                File.WriteAllText(path, summary.ToString() + details);
                Timeline.Log.LogMessage($"Start-up profile written to {path}\n{summary}");
            }
            catch (Exception exception)
            {
                Timeline.Log.LogError($"Could not write the start-up profile: {exception}");
            }
        }

        private static void Build(string reason, StringBuilder summary, StringBuilder details)
        {
            double end = Timeline.Now;
            List<Timeline.MarkEntry> marks;
            lock (Timeline.Marks)
            {
                marks = Timeline.Marks.OrderBy(mark => mark.time).ToList();
            }
            List<Timeline.LogLine> lines;
            lock (Timeline.Lines)
            {
                lines = Timeline.Lines.ToList();
            }
            double MarkTime(string prefix) => marks.FirstOrDefault(mark => mark.name.StartsWith(prefix))?.time ?? double.NaN;

            summary.AppendLine($"Start-up profile, written when {reason}, {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine("Times are main-thread wall-clock times unless marked as CPU, i.e. the time spent in that code itself.");
            summary.AppendLine();
            summary.AppendLine("== Overview ==");
            double patchersDone = MarkTime("Patchers finished");
            double chainloaderStart = MarkTime("Chainloader started");
            double chainloaderEnd = MarkTime("Chainloader finished");
            double gameStart = MarkTime("RoR2Application.Awake");
            double mainMenu = MarkTime("Main menu started");
            if (Timeline.BeforePreloader >= 0)
            {
                Row(summary, Timeline.BeforePreloader, "Unity's own start-up, before any mod (process start to BepInEx's patchers)");
            }
            Row(summary, patchersDone, "BepInEx's patchers, e.g. HookGenPatcher");
            Row(summary, chainloaderStart - patchersDone, "Patchers finished to the plugins starting to load");
            Row(summary, chainloaderEnd - chainloaderStart, "Loading the plugins (their Awake), see Plugins");
            Row(summary, gameStart - chainloaderEnd, "Plugins loaded to the game starting to load");
            Row(summary, mainMenu - gameStart, "The game loading, to the main menu, see Phases");
            Row(summary, (Timeline.BeforePreloader >= 0 ? Timeline.BeforePreloader : 0) + (double.IsNaN(mainMenu) ? end : mainMenu), "Total, from the process start");
            summary.AppendLine();

            // the plugins, each from BepInEx's "Loading [...]" to the next one or the end of the chainloader
            var plugins = new List<(string name, double time)>();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.source != "BepInEx" || !line.text.StartsWith("Loading [") || !line.text.EndsWith("]"))
                {
                    continue;
                }
                double next = chainloaderEnd;
                for (int j = i + 1; j < lines.Count; j++)
                {
                    if (lines[j].source == "BepInEx" && (lines[j].text.StartsWith("Loading [") || lines[j].text.StartsWith("Chainloader startup complete")))
                    {
                        next = lines[j].time;
                        break;
                    }
                }
                plugins.Add((line.text.Substring("Loading [".Length, line.text.Length - "Loading [".Length - 1), next - line.time));
            }
            plugins.Sort((a, b) => b.time.CompareTo(a.time));
            summary.AppendLine($"== Slowest plugins to load ({plugins.Count} plugins, {Format(plugins.Sum(plugin => plugin.time))} in all) ==");
            foreach (var (name, time) in plugins.Take(10))
            {
                Row(summary, time, name);
            }
            summary.AppendLine();

            var providers = Timeline.GetTimings(GamePatches.ProviderCategory)
                .GroupBy(timing => timing.name.Split(new[] { StepSeparator }, StringSplitOptions.None)[0])
                .Select(group => (name: group.Key, assembly: group.First().assembly, cpu: group.Sum(timing => timing.Milliseconds), steps: group.OrderByDescending(timing => timing.Milliseconds).ToList()))
                .OrderByDescending(provider => provider.cpu).ToList();
            summary.AppendLine($"== Slowest content pack providers, CPU ({providers.Count} providers, {Format(providers.Sum(provider => provider.cpu))} in all) ==");
            foreach (var provider in providers.Take(10))
            {
                Row(summary, provider.cpu, $"{provider.name} ({provider.assembly})");
            }
            summary.AppendLine();

            var initializers = Timeline.GetTimings(GamePatches.InitializerCategory).OrderByDescending(timing => timing.Milliseconds).ToList();
            summary.AppendLine($"== Slowest system initializers, CPU ({initializers.Count} initializers, {Format(initializers.Sum(timing => timing.Milliseconds))} in all) ==");
            foreach (var timing in initializers.Take(10))
            {
                Row(summary, timing.Milliseconds, $"{timing.name} ({timing.assembly})");
            }
            summary.AppendLine();

            details.AppendLine("== Timeline ==");
            details.AppendLine("Marks, and the game's start-up phases with how long they took and their CPU time.");
            var events = marks.Select(mark => (time: mark.time, text: mark.name)).ToList();
            foreach (var phase in Timeline.GetTimings(GamePatches.PhaseCategory))
            {
                events.Add((phase.firstStart, $"{phase.name}: {Format(phase.lastEnd - phase.firstStart)}, CPU {Format(phase.Milliseconds)}"));
            }
            foreach (var (time, text) in events.OrderBy(entry => entry.time))
            {
                details.AppendLine($"  at {Format(time),10}  {text}");
            }
            details.AppendLine();

            details.AppendLine("== Plugins, by the time to load them and run their Awake ==");
            foreach (var (name, time) in plugins.Take(TopCount))
            {
                Row(details, time, name);
            }
            details.AppendLine();

            details.AppendLine("== Content pack providers, CPU, with their steps ==");
            foreach (var provider in providers.Take(TopCount))
            {
                Row(details, provider.cpu, $"{provider.name} ({provider.assembly})");
                foreach (var step in provider.steps)
                {
                    var stepName = step.name.Split(new[] { StepSeparator }, StringSplitOptions.None).Last();
                    details.AppendLine($"      {Format(step.Milliseconds),10}  {stepName}, {Format(step.lastEnd - step.firstStart)} from first to last step");
                }
            }
            details.AppendLine();

            details.AppendLine("== System initializers, CPU, by assembly ==");
            foreach (var group in initializers.GroupBy(timing => timing.assembly).OrderByDescending(group => group.Sum(timing => timing.Milliseconds)))
            {
                Row(details, group.Sum(timing => timing.Milliseconds), $"{group.Key} ({group.Count()} initializers)");
            }
            details.AppendLine();
            details.AppendLine("== System initializers, CPU ==");
            foreach (var timing in initializers.Take(TopCount))
            {
                Row(details, timing.Milliseconds, $"{timing.name} ({timing.assembly})");
            }
            details.AppendLine();

            details.AppendLine($"== Longest frames, at least {Format(FrameWatcher.LongFrameThreshold)} ({FrameWatcher.Frames} frames since the plugins loaded) ==");
            details.AppendLine("Each with the last line logged before it ended.");
            foreach (var frame in FrameWatcher.LongFrames.OrderByDescending(frame => frame.duration).Take(TopCount))
            {
                details.AppendLine($"  {Format(frame.duration),10}  at {Format(frame.start)}: {Describe(Timeline.LastLineBefore(frame.start + frame.duration))}");
            }
            details.AppendLine();

            details.AppendLine("== Longest silences between log lines ==");
            details.AppendLine("Each with the line before and after it; what ran in between is what took the time.");
            var gaps = new List<(double gap, Timeline.LogLine before, Timeline.LogLine after)>();
            for (int i = 1; i < lines.Count; i++)
            {
                gaps.Add((lines[i].time - lines[i - 1].time, lines[i - 1], lines[i]));
            }
            foreach (var (gap, before, after) in gaps.OrderByDescending(entry => entry.gap).Take(TopCount))
            {
                details.AppendLine($"  {Format(gap),10}  at {Format(before.time)}");
                details.AppendLine($"              after:  {Describe(before)}");
                details.AppendLine($"              before: {Describe(after)}");
            }
        }

        private static void Row(StringBuilder builder, double milliseconds, string text)
        {
            builder.AppendLine($"  {Format(milliseconds),10}  {text}");
        }

        private static string Format(double milliseconds)
        {
            if (double.IsNaN(milliseconds))
            {
                return "?";
            }
            return milliseconds >= 1000
                ? (milliseconds / 1000).ToString("0.00", CultureInfo.InvariantCulture) + " s"
                : milliseconds.ToString("0", CultureInfo.InvariantCulture) + " ms";
        }

        private static string Describe(Timeline.LogLine line)
        {
            if (line == null)
            {
                return "(nothing logged)";
            }
            var text = line.text.Replace('\n', ' ').Replace('\r', ' ');
            if (text.Length > 200)
            {
                text = text.Substring(0, 200) + "...";
            }
            return $"[{line.source}] {text}";
        }
    }
}
