using Chrono.Graph.Core.Domain;
using System.Linq.Expressions;

namespace Chrono.Graph.Core.Application
{
    public interface IOptionalJoiner { 
        IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand);
        IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Clause clause);
        IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Action<IOptionalJoiner> deepJoiner);
        IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Clause clause, Action<IOptionalJoiner> deepJoiner);
    }
}
