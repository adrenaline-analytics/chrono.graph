using Chrono.Graph.Core.Domain;
using System.Linq.Expressions;

namespace Chrono.Graph.Core.Application
{
    public interface IQueryClause
    {
        Dictionary<string, Clause> Clauses { get; }

        IQueryClauseGroup Where<T, P>(Expression<Func<T, P?>> operand, Clause clause);
        IQueryClauseGroup Where<T>(string operand, Clause clause);
        IQueryClauseGroup Where(string operand, Clause clause, Type type);
        IQueryClauseGroup WhereGroup(Action<IQueryFactory> builder);
    }
}
