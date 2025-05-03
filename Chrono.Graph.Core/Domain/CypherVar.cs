using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Utilities;

namespace Chrono.Graph.Core.Domain
{
    public class CypherVar : Connectable
    {
        private string _label = string.Empty;
        private string _var = string.Empty;
        public string Hash { get; set; } = Utils.CypherId();
        public object? Object { get; set; }
        public Type? Type { get; set; }
        public string Label { 
            get => 
                GraphType == GraphObjectType.Edge 
                    ? Utils.StandardizeEdgeLabel(_label) 
                    : Utils.StandardizeNodeLabel(_label); 
            set => _label = value; 
        }
        public GraphObjectType GraphType { get; set; } = GraphObjectType.Node;
        public Dictionary<string, CypherVar> Connections { get; set; } = [];
        /// <summary>
        /// Used for filtering out children when calling Post, Put or Patch
        /// NOT used for Get or GetScalar
        /// </summary>
        public HashSet<string>? SaveChildFilter { get; set; }
        public string Var { get => Utils.StandardizeVariableName(string.IsNullOrEmpty(_var) ? $"node{_label}{Hash}" : _var); set => _var = value; }
    }
}
