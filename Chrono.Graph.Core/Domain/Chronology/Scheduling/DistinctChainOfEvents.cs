namespace Chrono.Graph.Core.Domain.Chronology.Scheduling
{
    public class DistinctChainOfEvents : DistinctChainOfEvents<float, float> { }
    public class DistinctChainOfEvents<IO, T> : ChainOfEvents<IO, T>
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Meta { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }

    }
}
