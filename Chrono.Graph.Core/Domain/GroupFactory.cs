using System.Linq.Expressions;
using Chrono.Graph.Core.Application;
using Chrono.Graph.Core.Utilities;

namespace Chrono.Graph.Core.Domain
{
    // Minimal factory to capture grouped Where/And/Or chains into ClauseGroups without touching cypher state
    public class GroupFactory : IQueryFactory
    {
        public Statement Statement { get; set; } = new Statement();
        public Dictionary<string, Clause> Clauses { get; set; } = new();
        public IEnumerable<ClauseGroup> SubClauses { get; set; } = new List<ClauseGroup>();
        public HashSet<string> JoinRegistry { get; set; } = new();

        // IQueryFactory
        public void Return() { }
        public void Return(Action<IJoiner> joiner) { }
        public void Return(CypherVar outVar) { }
        public Statement? Build() => null;
        public void HotLink<TT>(string verb, TT to) where TT : notnull { }
        public void HotLink<TT>(string verb, TT to, Action<IQueryClause> clause) where TT : notnull { }
        public IQueryFactory Command<T>(T connectTo, Action<IQueryFactory> builder, IQueryFactory factory) => this;
        public void ReturnStructured(Action<IJoiner> joiner) { }

        // IQueryClause
        public IQueryClauseGroup All()
        {
            var subclause = new ClauseGroup();
            SubClauses = SubClauses.Append(subclause);
            return subclause;
        }
        public IQueryClauseGroup Where<T, P>(Expression<Func<T, P?>> operand, Clause clause) => Where(operand.GetExpressionPropertyName(), clause, typeof(T));
        public IQueryClauseGroup Where<T>(string operand, Clause clause) => Where(operand, clause, typeof(T));
        public IQueryClauseGroup Where(string operand, Clause clause, Type type)
        {
            var subclause = new ClauseGroup();
            var label = ObjectHelper.GetPropertyLabel(type, operand);
            subclause.Clauses.Add(label, clause);
            SubClauses = SubClauses.Append(subclause);
            return subclause;
        }
        public IQueryClauseGroup WhereGroup(Action<IQueryFactory> builder)
        {
            var inner = new GroupFactory();
            builder(inner);
            foreach (var group in inner.SubClauses)
                SubClauses = SubClauses.Append(group);
            // Return a fresh group for potential chaining
            var subclause = new ClauseGroup();
            SubClauses = SubClauses.Append(subclause);
            return subclause;
        }

        // ISubQueryFactory
        public string Hash => "grp";
        public Dictionary<int, IQueryFactory> GlobalObjectRegistry { get; } = new();
        public Dictionary<string, HashSet<GraphEdgeBasic>> InboundEdges { get; set; } = new();
        public CypherVar RootVar { get; set; } = new CypherVar { Var = "grp" };
        public void Create(object? thing) => throw new NotImplementedException();
        public void CreateChild(object? parent, object? child, System.Reflection.PropertyInfo? prop) => throw new NotImplementedException();
        public void CreateChild(object? parent, object? child, System.Reflection.PropertyInfo? prop, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void CreateChild(object? parent, object? child, System.Reflection.PropertyInfo? prop, Action<ISubQueryFactory> build, string label) => throw new NotImplementedException();
        public void CreateChild(object? parent, object? child, System.Reflection.PropertyInfo? prop, Action<ISubQueryFactory> builder, Func<ISubQueryFactory, GraphEdgeDetails> edgeDefiner) => throw new NotImplementedException();
        public void Merge<T>(T thing, Action<IQueryClause> clausation, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void Merge<T>(T thing, string label, Action<IQueryClause> clausation, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void Merge<T>(T thing, System.Reflection.PropertyInfo property, Action<IQueryClause> clausation, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void Match<T>(T thing, Action<IQueryClause> clausation, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void Match<T>(T thing, string label, Action<IQueryClause> clausation, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void Match<T>(T thing, System.Reflection.PropertyInfo property, Action<IQueryClause> clausation, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void MatchChild<A, B>(A from, B connectTo, Action<IQueryClause> clausation, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void MatchChild<A, B>(A from, B connectTo, System.Reflection.PropertyInfo property, Action<IQueryClause> clausation, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void MatchChild<A, B>(A parent, B child, Action<IQueryClause> clausation, Action<ISubQueryFactory> build, Func<ISubQueryFactory, GraphEdgeDetails> edgeDefiner) => throw new NotImplementedException();
        public void MergeChild<A, B>(A parent, B child, Action<IQueryClause> clausation, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void MergeChild<A, B>(A parent, B child, System.Reflection.PropertyInfo property, Action<IQueryClause> clausation, Action<ISubQueryFactory> builder) where A : notnull => throw new NotImplementedException();
        public void MergeChild<A, B>(A parent, B child, Action<IQueryClause> clausation, Action<ISubQueryFactory> builder, Func<ISubQueryFactory, GraphEdgeDetails> edgeDefiner) where A : notnull => throw new NotImplementedException();
        public void Delete() => throw new NotImplementedException();
        public void Delete(CypherVar outVar) => throw new NotImplementedException();
        public void OptionalMatch<T>(T thing, bool idempotent) => throw new NotImplementedException();
        public void OnCreateSet<T>(T thing) where T : notnull => throw new NotImplementedException();
        public void OnMatchSet<T>(T thing, bool idempotent) where T : notnull => throw new NotImplementedException();
        public void Match(IEnumerable<CypherVar> cypherVars) => throw new NotImplementedException();
        public void Match(CypherVar cypherVar) => throw new NotImplementedException();

        // IJoiner
        public IJoiner Join<T, P>(Expression<Func<T, P?>> operand) => throw new NotImplementedException();
        public IJoiner Join<T, P>(Expression<Func<T, P>> operand, Action<IQueryClause> clause) => throw new NotImplementedException();
        public IJoiner Join<T, P>(Expression<Func<T, P>> operand, Action<IJoiner> deepJoiner) => throw new NotImplementedException();
        public IJoiner Join<T, P>(Expression<Func<T, P>> operand, Action<IQueryClause> clause, Action<IJoiner> deepJoiner) => throw new NotImplementedException();
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand) => throw new NotImplementedException();
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Action<IQueryClause> clause) => throw new NotImplementedException();
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Action<IJoiner> deepJoiner) => throw new NotImplementedException();
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Action<IQueryClause> clause, Action<IJoiner> deepJoiner) => throw new NotImplementedException();
        public void RemoveStaleConnections<T>(T thing, bool deep) where T : class => throw new NotImplementedException();
    }
}
