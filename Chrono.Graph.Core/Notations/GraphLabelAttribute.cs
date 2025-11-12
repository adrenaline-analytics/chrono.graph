namespace Chrono.Graph.Core.Notations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class GraphLabelAttribute : Attribute
    {
        public string Label { get; private set; }
        public bool Secondary { get; private set; }
        public GraphLabelAttribute(string label, bool secondary = false)
        {
            Label = label;
            Secondary = secondary;
        }
    }
}
