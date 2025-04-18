namespace Chrono.Graph.Core.Domain.Chronology
{
    public class Countdown : Counter
    {
        public Countdown() : base() { }
        public Countdown(long? runTime) : base(runTime) { }
        public float? LastKnownRemainingTime { get; set; }
    }
}
