namespace Chrono.Graph.Core.Domain.Chronology.Scheduling
{
    public class DistinctChainOfEvents : DistinctChainOfEvents<float, float> {
        public DistinctChainOfEvents() : base(0f, 0f, []) { }
        public DistinctChainOfEvents(float input, float target, IList<LinkedEvent<float, float>> origin) : base(input, target, origin) { }  
    }
    public class DistinctChainOfEvents<IO, T> : ChainOfEvents<IO, T>
    {
        public DistinctChainOfEvents(IO input, T target, IList<LinkedEvent<IO, T>> origin) : base(input, target, origin) { }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Meta { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }

    }
}
