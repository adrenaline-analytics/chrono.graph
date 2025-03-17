using Chrono.Graph.Core.Domain;
using System.Linq.Expressions;

namespace Chrono.Graph.Core.Application
{
    public interface IJoiner
    {
        CypherVar RootVar { get; }
        IJoiner Join<T, P>(Expression<Func<T, P?>> operand);
        IJoiner Join<T, P>(Expression<Func<T, P>> operand, Clause clause);
        IJoiner Join<T, P>(Expression<Func<T, P>> operand, Action<IJoiner> deepJoiner);
        IJoiner Join<T, P>(Expression<Func<T, P>> operand, Clause clause, Action<IJoiner> deepJoiner);
        IJoiner JoinAllChildren();
        IJoiner JoinAllChildrenRecursive(int depth);
        IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand);
        IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Clause clause);
        IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Action<IJoiner> deepJoiner);
        IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Clause clause, Action<IJoiner> deepJoiner);
    }
}
