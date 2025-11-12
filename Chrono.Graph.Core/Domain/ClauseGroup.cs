using System.Linq.Expressions;
using Chrono.Graph.Core.Application;
using Chrono.Graph.Core.Utilities;

namespace Chrono.Graph.Core.Domain
{
    public partial class ClauseGroup : IQueryClauseGroup
    {
        public Dictionary<string, Clause> Clauses { get; set; } = new Dictionary<string, Clause>();
        public IEnumerable<ClauseGroup> SubClauses { get; set; } = new List<ClauseGroup>();
        public ClauseGroup()
        {

        }
        public IQueryClauseGroup And<T, TT>(Expression<Func<T, TT>> operand, Clause clause) => And<T>(operand.GetExpressionPropertyName(), clause);
        public IQueryClauseGroup And<T>(string operand, Clause clause) => And(operand, clause, typeof(T));
        public IQueryClauseGroup And(string operand, Clause clause, Type type)
        {
            var subclause = new ClauseGroup();
            Clauses.Add(ObjectHelper.GetPropertyLabel(type, operand), clause);
            SubClauses = SubClauses.Append(subclause);
            return subclause;

        }
        public IQueryClauseGroup Or<T, TT>(Expression<Func<T, TT>> operand, Clause clause)
        {
            var subclause = new ClauseGroup();
            // Mark as group expression for OR chains; Neo4jFactory will format as parentheses
            clause.IsGroupOrExpression = true;
            Clauses.Add(ObjectHelper.GetPropertyLabel(typeof(T), operand.GetExpressionPropertyName()), clause);
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
                if (!(group.Clauses?.Values.Any(c => c.IsGroupOrExpression) ?? false))
                {
                    foreach (var kv in group.Clauses.ToArray())
                        group.Clauses[kv.Key].IsGroupOrExpression = true;
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
