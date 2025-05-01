namespace Chrono.Graph.Core.Domain.Chronology
{
    public class Counter
    {
        public Counter() { }
        public Counter(long? runTime, long? runTicks)
        {
            RunTime = runTime;
            RunTicks = runTicks;
        }
        public Counter(long? runTime)
        {
            RunTime = runTime;
        }
        /// <summary>
        /// Time it was started
        /// </summary>
        public long? RunTime { get; set; }

        /// <summary>
        /// Counted down from or counted up to...
        ///   < 0  - countdown
        ///   = 0  - timestamp
        ///   > 0  - stopwatch
        ///   null - live stopwatch
        /// </summary>
        public long? RunTicks { get; set; }

        /// <summary>
        /// IF PAUSED, time it was paused
        /// </summary>
        public long? PauseTime { get; set; }

        /// <summary>
        /// Cumulative time ellapsed while paused
        /// </summary>
        public long? PauseTicks { get; set; }

        public bool IsPaused => PauseTime != null;
        public long? LastSyncedTime { get; set; }
    }
}
