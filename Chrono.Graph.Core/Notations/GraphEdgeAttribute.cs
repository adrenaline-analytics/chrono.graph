using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Domain;

namespace Chrono.Graph.Core.Notations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Field)]
    public class GraphSquishAttribute : Attribute
    {
        ///Put all child properties in parent object as child.property / child[index].property 
        ///connect child edges to parent
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Field)]
    public class GraphEdgeAttribute : Attribute
    {
        public GraphEdgeDetails? Definition { get; set; }
        //public string EdgeLabel { get; private set; }
        //public string? ObjectLabel { get; set; }
        //public GraphEdgeDirection Direction { get; private set; }
        private void Make(string label, GraphEdgeDirection direction)
            => Definition = new GraphEdgeDetails
            {
                Label = label,
                Direction = direction
            };

        public GraphEdgeAttribute(GraphEdgeDirection direction) => Make("", direction);
        public GraphEdgeAttribute(string label, GraphEdgeDirection direction) => Make(label, direction);
        public GraphEdgeAttribute(string label) => Make(label, GraphEdgeDirection.Out);

    }
}
