namespace Chrono.Graph.Core.Domain.Chronology.Scheduling
{
    public class Schedule : Schedule<float, float> { }
    public class Schedule<IO, T> : DistinctChainOfEvents<IO, T> { }
}
