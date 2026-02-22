using System.Linq.Expressions;
using Chrono.Graph.Core.Application;
using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Utilities;
using NanoidDotNet;

namespace Chrono.Graph.Core.Domain
{
    public partial class ClauseGroup : IQueryClauseGroup
    {
        public List<Clause> Clauses { get; set; } = [];
        public IEnumerable<ClauseGroup> SubClauses { get; set; } = new List<ClauseGroup>();
        public ClauseGroup()
        {

        }
        public IQueryClauseGroup And<T, TT>(Expression<Func<T, TT>> operand, Clause clause) => And<T>(operand.GetExpressionPropertyName(), clause);
        public IQueryClauseGroup And<T>(string operand, Clause clause) => And(operand, clause, typeof(T));
        public IQueryClauseGroup And(string operand, Clause clause, Type type)
        {
            var subclause = new ClauseGroup();
            clause.PropertyLabel = ObjectHelper.GetPropertyLabel(type, operand);
            clause.Hash = Nanoid.Generate(CypherConstants.SafeAlphabet, CypherConstants.SafeIdLength);
            Clauses.Add(clause);
            SubClauses = SubClauses.Append(subclause);
            return subclause;

        }
        public IQueryClauseGroup Or<T, TT>(Expression<Func<T, TT>> operand, Clause clause)
        {
            var subclause = new ClauseGroup();
            // Mark as group expression for OR chains; Neo4jFactory will format as parentheses
            clause.IsGroupOrExpression = true;
            clause.PropertyLabel = ObjectHelper.GetPropertyLabel(typeof(T), operand.GetExpressionPropertyName());
            clause.Hash = Nanoid.Generate(CypherConstants.SafeAlphabet, CypherConstants.SafeIdLength);
            Clauses.Add(clause);
            SubClauses = SubClauses.Append(subclause);
            return subclause;
        }
        public IQueryClauseGroup OrGroup(Action<IQueryFactory> builder)
        {
            // Build a grouped set of predicates where the inner group's predicates
            // are combined with OR semantics
            var factory = new GroupFactory();
            builder(factory);

            // Ensure OR semantics within each produced group
            foreach (var group in factory.SubClauses)
            {
                if (!(group.Clauses?.Any(c => c.IsGroupOrExpression) ?? false))
                {
                    foreach (var clause in group.Clauses ?? [])
                        clause.IsGroupOrExpression = true;
                }
                SubClauses = SubClauses.Append(group);
            }
            return this;
        }
        public IQueryClauseGroup AndGroup(Action<IQueryFactory> builder)
        {
            // Build a grouped set of predicates and append as its own group.
            // Top-level groups are AND'ed together by the query factory.
            var factory = new GroupFactory();
            builder(factory);
            foreach (var group in factory.SubClauses)
                SubClauses = SubClauses.Append(group);
            return this;
        }
    }
}
