using Chrono.Graph.Core.Domain;
using System.Reflection;

namespace Chrono.Graph.Core.Application
{
    public interface ISubQueryFactory : IQueryClause, IJoiner
    {
        string Hash { get; }
        Dictionary<int, IQueryFactory> GlobalObjectRegistry { get; }
        Dictionary<string, HashSet<GraphEdgeBasic>> InboundEdges { get; set; }
        IEnumerable<ClauseGroup> SubClauses { get; set; }

        void Create(object? thing);

        void CreateChild(object? parent, object? child, PropertyInfo? prop);
        void CreateChild(object? parent, object? child, PropertyInfo? prop, Action<ISubQueryFactory> build);
        void CreateChild(object? parent, object? child, PropertyInfo? prop, Action<ISubQueryFactory> builder, string label);
        void CreateChild(object? parent, object? child, PropertyInfo? prop, Action<ISubQueryFactory> build, Func<GraphEdgeDetails> edgeDefiner);

        void Merge<T>(T thing, Action<IQueryClause> clausation, Action<ISubQueryFactory> build);
        void Merge<T>(T thing, string label, Action<IQueryClause> clausation, Action<ISubQueryFactory> build);
        void Merge<T>(T thing, PropertyInfo property, Action<IQueryClause> clausation, Action<ISubQueryFactory> build);

        void MergeChild<A, B>(A parent, B child, Action<IQueryClause> clausation, Action<ISubQueryFactory> build);
        void MergeChild<A, B>(A parent, B child, PropertyInfo property, Action<IQueryClause> clausation, Action<ISubQueryFactory> build);

        void Match<T>(T thing, Action<IQueryClause> clausation, Action<ISubQueryFactory> build);
        void Match<T>(T thing, string label, Action<IQueryClause> clausation, Action<ISubQueryFactory> build);
        void Match<T>(T thing, PropertyInfo property, Action<IQueryClause> clausation, Action<ISubQueryFactory> build);

        void MatchChild<A, B>(A parent, B child, Action<IQueryClause> clausation, Action<ISubQueryFactory> build);
        void MatchChild<A, B>(A parent, B child, PropertyInfo property, Action<IQueryClause> clausation, Action<ISubQueryFactory> build);
        void MatchChild<A, B>(A parent, B child, Action<IQueryClause> clausation, Action<ISubQueryFactory> build, Func<GraphEdgeDetails> edgeDefiner);

        void Delete();
        void Delete(CypherVar outVar);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <param name="idempotent"></param>
        /// <param name="idempotent">If null values are present on thing properties, delete them from the graph object - 
        /// forcing full overwrite & idempotency</param>
        void OptionalMatch<T>(T thing, bool idempotent);
        void OnCreateSet<T>(T thing);

        /// <summary>
        /// Create cypher commands for a match statement
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <param name="idempotent">If null values are present on thing properties, delete them from the graph object - 
        /// forcing full overwrite & idempotency</param>
        void OnMatchSet<T>(T thing, bool idempotent);
        void Match(IEnumerable<CypherVar> cypherVars);
        void Match(CypherVar cypherVar);
        void MergeChild<A, B>(A parent, B child, Action<IQueryClause> clauser, Action<ISubQueryFactory> builder, Func<GraphEdgeDetails> edgeDefiner);
    }

}
