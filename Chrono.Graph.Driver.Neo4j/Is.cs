using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Domain;

namespace Chrono.Graph.Adapter.Neo4j
{
    public static class Is
    {
        public static Clause Equal<T>(T operand)
            => new Clause
            {
                Operand = operand,
                Operator = CypherConstants.EqualsOperator
            };

        public static Clause In<T>(IEnumerable<T> operands)
            => new Clause
            {
                Operand = operands,
                Operator = CypherConstants.InOperator
            };
        public static Clause NotIn<T>(IEnumerable<T> operands)
            => new Clause
            {
                Operand = operands,
                Operator = CypherConstants.NotInOperator
            };
        public static Clause NotEqual<T>(T operand)
            => new Clause
            {
                Operand = operand,
                Operator = CypherConstants.NotEqualsOperator
            };
        public static Clause GreaterThan<T>(T operand)
            => new Clause
            {
                Operand = operand,
                Operator = CypherConstants.GreaterThanOperator
            };
        public static Clause GreaterThanOrEqual<T>(T operand)
            => new Clause
            {
                Operand = operand,
                Operator = CypherConstants.GreaterThanOrEqualOperator
            };
        public static Clause LessThan<T>(T operand)
            => new Clause
            {
                Operand = operand,
                Operator = CypherConstants.LessThanOperator
            };
        public static Clause LessThanOrEqual<T>(T operand)
            => new Clause
            {
                Operand = operand,
                Operator = CypherConstants.LessThanOrEqualOperator
            };
        public static Clause Containing<T>(T operand)
            => new Clause
            {
                Operand = operand,
                Operator = CypherConstants.ContainsOperator
            };
        public static Clause ContainingKey<T>(T operand)
            => new Clause
            {
                Operand = operand,
                Operator = CypherConstants.ContainsOperator
            };
        public static Clause Exists() => new Clause
        {
            Operand = null,
            Operator = CypherConstants.ExistsFunction
        };
        public static Clause Null() => new Clause
        {
            Operand = null,
            Operator = CypherConstants.IsNullOperator
        };
        public static Clause NotNull() => new Clause
        {
            Operand = null,
            Operator = CypherConstants.IsNotNullOperator
        };
        public static Clause Not(Clause inner)
            => new Clause
            {
                Operand = inner,
                Operator = CypherConstants.NotOperator
            };
    }
}
