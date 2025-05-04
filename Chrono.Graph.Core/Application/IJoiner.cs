using Chrono.Graph.Core.Domain;
using System.Linq.Expressions;

namespace Chrono.Graph.Core.Application
{
    public interface IJoiner
    {
        CypherVar RootVar { get; }
        HashSet<string> JoinRegistry { get; set; }

        IJoiner Join<T, P>(Expression<Func<T, P?>> operand);
        IJoiner Join<T, P>(Expression<Func<T, P>> operand, Clause clause);
        IJoiner Join<T, P>(Expression<Func<T, P>> operand, Action<IJoiner> deepJoiner);
        IJoiner Join<T, P>(Expression<Func<T, P>> operand, Clause clause, Action<IJoiner> deepJoiner);
        //IJoiner JoinAllChildren(object thing);
        //IJoiner JoinAllChildrenRecursive(object thing, int depth);
        IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand);
        IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Clause clause);
        IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Action<IJoiner> deepJoiner);
        IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Clause clause, Action<IJoiner> deepJoiner);
    }
}
