using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Notations;
using Chrono.Graph.Core.Utilities;

namespace Chrono.Graph.Core.Domain
{
    public class GraphEdgeDetails : GraphEdgeBasic
    {
        private string _label = string.Empty;

        public string Hash { get; } = Utils.CypherId();
        public override string Label { get => Utils.StandardizeEdgeLabel(_label); set => _label = value; }
        public string Var { get => Utils.StandardizeVariableName($"edge{_label ?? ""}{Hash}"); }
        public bool? Optional { get; set; }
        public GraphEdgeDirection Direction { get; set; } = GraphEdgeDirection.Out;
        public GraphKeyLabellingAttribute? KeyLabelling { get; internal set; }
    }
}
