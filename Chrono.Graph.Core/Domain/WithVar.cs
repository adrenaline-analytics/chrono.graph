namespace Chrono.Graph.Core.Domain
{
    public class WithVar
    {
        public string Var { get; set; } = string.Empty;
        public string? Construct { get; set; }
        public List<string>? Collapses { get; set; }
        public string? EdgeVar { get; internal set; }
    }
}
