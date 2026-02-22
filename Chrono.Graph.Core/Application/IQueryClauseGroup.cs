using Chrono.Graph.Core.Domain;
using System.Linq.Expressions;

namespace Chrono.Graph.Core.Application
{
    public interface IQueryClauseGroup
    {
        List<Clause> Clauses { get; set; }
        IEnumerable<ClauseGroup> SubClauses { get; set; }
        IQueryClauseGroup And<T, TT>(Expression<Func<T, TT?>> operand, Clause clause);
        IQueryClauseGroup AndGroup(Action<IQueryFactory> builder);
        IQueryClauseGroup Or<T, TT>(Expression<Func<T, TT?>> operand, Clause clause);
        IQueryClauseGroup OrGroup(Action<IQueryFactory> builder);
    }
}
