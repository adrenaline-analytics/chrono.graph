using Chrono.Graph.Core.Domain;

namespace Chrono.Graph.Core.Application
{
    public interface IQueryFactory : ISubQueryFactory
    {
        void Return();
        void Return(Action<IJoiner> joiner);
        void Return(CypherVar outVar);
        Statement? Build();
        void HotLink<TT>(string verb, TT to);
        void HotLink<TT>(string verb, TT to, Action<IQueryClause> clause);
        IQueryFactory Command<T>(T connectTo, Action<IQueryFactory> builder, IQueryFactory factory);

        Statement Statement { get; set; }
        Dictionary<string, Clause> Clauses { get; set; }
    }

}
