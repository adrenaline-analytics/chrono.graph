namespace Chrono.Graph.Notations
{
    /// <summary>
    /// Usable on Dictionaries, uses the key as the edge label instead of the property name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class GraphKeyLabellingAttribute : Attribute { }
}
