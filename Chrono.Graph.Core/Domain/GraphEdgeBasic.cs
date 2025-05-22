using Chrono.Graph.Core.Constant;

namespace Chrono.Graph.Core.Domain
{
    public class GraphEdgeBasic
    {
        public virtual string Label { get; set; } = string.Empty;
        public Dictionary<string, string> Properties { get; set; } = [];
        public override bool Equals(object? obj) => GetHashCode() == obj?.GetHashCode();
        public override int GetHashCode() => HashCode.Combine(Label);
    }
}
