namespace Chrono.Graph.Core.Domain.Chronology
{
    public class Countdown : Counter
    {
        public Countdown() : base() { }
        public Countdown(long? runTime, long? runTicks) : base(runTime, runTicks) { }
        public float? LastKnownRemainingTime { get; set; }
    }
}
