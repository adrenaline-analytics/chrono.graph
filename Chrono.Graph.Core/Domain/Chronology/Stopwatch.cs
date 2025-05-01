namespace Chrono.Graph.Core.Domain.Chronology
{
    public class Stopwatch : Counter
    {
        public Stopwatch() : base() { }
        public Stopwatch(long? runTime, long? runTicks) : base(runTime, runTicks) { }
        public Stopwatch(long? runTime) : base(runTime) { }
    }

}
