namespace Chrono.Graph.Notations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class GraphLabelAttribute : Attribute {
        public string Label { get; private set; }
        public GraphLabelAttribute(string label) 
        { 
            Label = label;
        }
    }
}
