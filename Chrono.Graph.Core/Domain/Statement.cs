using Chrono.Graph.Core.Utilities;

namespace Chrono.Graph.Core.Domain
{
    public class Statement
    {
        public string Cypher { get; set; } = string.Empty;
        public string NodeCypher { get; set; } = string.Empty;
        public string EdgeCypher { get; set; } = string.Empty;
        public string[] Commands { get; set; } = []; //MATCH, MERGE, etc
        public string Query { get; set; } = string.Empty;
        //public string[] Actions { get; set; } = []; //RETURN, DELETE, UPDATE ON
        public string[] Returns { get; set; } = [];
        public string[] Deletes { get; set; } = [];
        public Dictionary<string, string> DoOns { get; set; } = [];

        public Dictionary<string, CypherVar> OutVars { get; set; } = new Dictionary<string, CypherVar>();
        public Dictionary<string, CypherVar> InVars { get; set; } = new Dictionary<string, CypherVar>();
        public string[] Withs { get; set; } = [];

        //node var, properties

        public static Statement operator + (Statement them, Statement me)
        {
            them.InVars.Merge(me.InVars);
            them.OutVars.Merge(me.OutVars);
            them.Commands = me.Commands;
            them.Returns = me.Returns;
            them.Deletes = me.Deletes;
            them.DoOns = me.DoOns;
            them.Query = me.Query;
            return them;
        }
    }
}
