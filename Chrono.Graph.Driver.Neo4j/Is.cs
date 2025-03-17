using Chrono.Graph.Core.Application;
using Chrono.Graph.Core.Domain;

namespace Chrono.Graph.Adapter.Neo4j
{
    public static class Is
    {
        public static Clause Equal<T>(T operand) 
            => new Clause {
                Operand = operand,
                Operator = CypherConstants.EqualsOperator
            };

        public static Clause In<T>(IEnumerable<T> operands)
            => new Clause {
                Operand = operands,
                Operator = CypherConstants.InOperator
            };
        public static Clause Containing<T>(T operand)
            => new Clause {
                Operand = operand,
                Operator = CypherConstants.InOperator
            };
        public static Clause ContainingKey<T>(T operand)
            => new Clause {
                Operand = operand,
                Operator = CypherConstants.InOperator
            };
    }
}
