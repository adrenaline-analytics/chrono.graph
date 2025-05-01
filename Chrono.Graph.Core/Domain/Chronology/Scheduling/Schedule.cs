namespace Chrono.Graph.Core.Domain.Chronology.Scheduling
{
    public class Schedule : Schedule<float, float>
    {
        public Schedule() : base(0f, 0f, []) { }
        public Schedule(float input, float target, IList<LinkedEvent<float, float>> origin) : base(input, target, origin) { }
    }
    public class Schedule<IO, T> : DistinctChainOfEvents<IO, T>
    {
        public Schedule(IO input, T target, IList<LinkedEvent<IO, T>> origin) : base(input, target, origin) 
        {
        }
    }
}
