namespace Chrono.Graph.Core.Domain
{
    public class Connectable
    {
        public GraphEdgeDetails? Edge { get; set; }
        public Dictionary<string, Connectable> Connections { get; set; } = [];
    }
}
