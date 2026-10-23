using System.Collections.Generic;
using UnityEngine;

namespace StartupProfiler
{
    // Notes the frames that took long, i.e. where the game stood still loading something
    public class FrameWatcher : MonoBehaviour
    {
        public class LongFrame
        {
            public double start;
            public double duration;
        }

        public const double LongFrameThreshold = 100;
        public static readonly List<LongFrame> LongFrames = new List<LongFrame>();
        public static int Frames { get; private set; }
        private double lastFrame = -1;

        public static void Create()
        {
            var gameObject = new GameObject("StartupProfiler");
            DontDestroyOnLoad(gameObject);
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            gameObject.AddComponent<FrameWatcher>();
        }

        private void Update()
        {
            double now = Timeline.Now;
            if (lastFrame >= 0)
            {
                Frames++;
                if (now - lastFrame >= LongFrameThreshold)
                {
                    LongFrames.Add(new LongFrame { start = lastFrame, duration = now - lastFrame });
                }
            }
            lastFrame = now;
            if (Report.Written)
            {
                Destroy(gameObject);
            }
        }

        // e.g. the game closed or crashed before the main menu
        private void OnApplicationQuit()
        {
            Report.Write("the game was closed before the main menu");
        }
    }
}
