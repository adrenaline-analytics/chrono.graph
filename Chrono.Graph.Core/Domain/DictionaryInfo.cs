using Chrono.Graph.Core.Constant;

namespace Chrono.Graph.Core.Domain
{
    public class DictionaryInfo
    {
        public Type KeyType { get; set; }
        public Type ValType { get; set; }
        public GraphPrimitivity KeyPrimitivity { get; set; }
        public GraphPrimitivity ValPrimitivity { get; set; }
    }
}
