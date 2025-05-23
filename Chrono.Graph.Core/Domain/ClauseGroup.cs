﻿using Chrono.Graph.Core.Application;
using Chrono.Graph.Core.Utilities;
using System.Linq.Expressions;

namespace Chrono.Graph.Core.Domain
{
    public class ClauseGroup : IQueryClauseGroup
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
            return new ClauseGroup();
        }
        public IQueryClauseGroup OrGroup(Action<IQueryFactory> builder)
        {
            return new ClauseGroup();
        }
        public IQueryClauseGroup AndGroup(Action<IQueryFactory> builder)
        {
            return new ClauseGroup();
        }
    }
}
