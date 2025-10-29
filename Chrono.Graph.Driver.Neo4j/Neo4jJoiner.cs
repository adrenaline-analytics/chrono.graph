using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Chrono.Graph.Core.Application;
using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Domain;
using Chrono.Graph.Core.Notations;
using Chrono.Graph.Core.Utilities;

namespace Chrono.Graph.Adapter.Neo4j
{
    /// <summary>
    /// If one instance matches multiple join filters
    /// aka the same object instance exists in multiple properties or child properties of object,
    /// it will go with the first one in the stack defined in code
    /// </summary>
    public class Neo4jJoiner : IJoiner
    {
        public HashSet<string> JoinRegistry { get; set; } = [];
        public CypherVar RootVar { get; protected set; } = new();

        private Neo4jJoiner JoinRoller<T, P>(Expression<Func<T, P>> operand, Action<IQueryClause> clause, Action<IJoiner> deepJoiner, bool optional)
            => JoinRoller(operand.GetExpressionProperty(), clause, deepJoiner, optional);
        private Neo4jJoiner JoinRoller(PropertyInfo member, Action<IQueryClause> clause, Action<IJoiner> deepJoiner, bool optional)
        {
            RootVar.SaveChildFilter ??= [];
            RootVar.SaveChildFilter.Add(member.Name);

            var primitivity = ObjectHelper.GetPrimitivity(member.PropertyType);
            var objectLabel = ObjectHelper.GetObjectLabel(member.PropertyType);
            if (primitivity.HasFlag(GraphPrimitivity.Dictionary))
            {
                var dicInfo = ObjectHelper.GetDictionaryInfo(member.PropertyType);
                if ((dicInfo?.KeyType?.IsEnum ?? false) && member.GetCustomAttribute<GraphKeyLabellingAttribute>() != null)
                {
                    foreach (var enumFieldLabel in ObjectHelper.GetDictionaryLabels(dicInfo, member))
                    {
                        var enumFieldSubfactory = new Neo4jJoiner();
                        enumFieldSubfactory.RootVar = new CypherVar
                        {
                            Type = member.PropertyType,
                            Var = $"{objectLabel}{Utils.CypherId()}",
                            Edge = ObjectHelper.GetPropertyEdge(member, true, enumFieldLabel), //must always be OPTIONAL MATCH
                            Label = objectLabel,
                            GraphType = GraphObjectType.Node,
                        };
                        enumFieldSubfactory.JoinRegistry = [.. JoinRegistry];
                        deepJoiner(enumFieldSubfactory);
                        RootVar.Connections[$"{member.Name}.{Utils.StandardizeEdgeLabel(enumFieldLabel)}"] = enumFieldSubfactory.RootVar;
                    }
                    return this;

                }

            }
            var subfactory = new Neo4jJoiner();
            subfactory.RootVar = new CypherVar
            {
                Type = member.PropertyType,
                Var = $"{objectLabel}{Utils.CypherId()}",
                Edge = ObjectHelper.GetPropertyEdge(member, optional),
                Label = objectLabel,
                GraphType = GraphObjectType.Node,
            };
            subfactory.JoinRegistry = [.. JoinRegistry];
            // Apply clause to child variable if provided
            if (clause != null)
            {
                var childClause = new ChildQueryClause(subfactory.RootVar);
                clause(childClause);
            }
            deepJoiner(subfactory);
            RootVar.Connections[member.Name] = subfactory.RootVar;

            return this;
        }

        private class ChildQueryClause : IQueryClause
        {
            private readonly CypherVar _var;
            public ChildQueryClause(CypherVar v) { _var = v; }
            public Dictionary<string, Clause> Clauses => _var.Clauses;
            public IQueryClauseGroup All()
            {
                var sub = new ClauseGroup();
                _var.SubClauses = _var.SubClauses.Append(sub);
                return sub;
            }
            public IQueryClauseGroup Where<T, P>(Expression<Func<T, P?>> operand, Clause clause) => Where(operand.GetExpressionPropertyName(), clause, typeof(T));
            public IQueryClauseGroup Where<T>(string operand, Clause clause) => Where(operand, clause, typeof(T));
            public IQueryClauseGroup Where(string operand, Clause clause, Type type)
            {
                if (!_var.Clauses.TryAdd(ObjectHelper.GetPropertyLabel(type, operand), clause)
                    && _var.Clauses.TryGetValue(ObjectHelper.GetPropertyLabel(type, operand), out var existing)
                    && !existing.Equals(clause))
                    throw new ArgumentException("A data collision has occurred when attempting to build a clause");

                var sub = new ClauseGroup();
                _var.SubClauses = _var.SubClauses.Append(sub);
                return sub;
            }
            public IQueryClauseGroup WhereGroup(Action<IQueryFactory> builder)
            {
                return new ClauseGroup();
            }
        }

        public IJoiner Join<T, P>(Expression<Func<T, P?>> operand) => Join(operand, _ => { }, _ => { });
        public IJoiner Join<T, P>(Expression<Func<T, P>> operand, Action<IQueryClause> clause) => Join(operand, clause, _ => { });
        public IJoiner Join<T, P>(Expression<Func<T, P>> operand, Action<IJoiner> deepJoiner) => Join(operand, _ => { }, deepJoiner);
        public IJoiner Join<T, P>(Expression<Func<T, P>> operand, Action<IQueryClause> clause, Action<IJoiner> deepJoiner) => JoinRoller(operand, clause, deepJoiner, false);
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand) => JoinOptional(operand, _ => { }, _ => { });
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Action<IQueryClause> clause) => JoinOptional(operand, clause, _ => { });
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Action<IJoiner> deepJoiner) => JoinOptional(operand, _ => { }, deepJoiner);
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Action<IQueryClause> clause, Action<IJoiner> deepJoiner) => JoinRoller(operand, clause, deepJoiner, true);
        //public IJoiner JoinAllChildren(object thing) => JoinAllChildrenRecursive(thing, 1);
        //public IJoiner JoinAllChildrenRecursive(object thing, int depth)
        //{
        //    if (thing != null && depth > 0)
        //    {
        //        foreach (var prop in RootVar.Type?.GetProperties() ?? [])
        //        {
        //            if (prop.Name == "Headquarters")
        //            {
        //                Debug.WriteLine("Break");
        //            }
        //            var primitivity = ObjectHelper.GetPrimitivity(prop.PropertyType);
        //            var serializable = ObjectHelper.IsSerializable(prop);
        //            var ignore = (prop.GetCustomAttribute<GraphIgnoreAttribute>() ?? prop.PropertyType.GetCustomAttribute<GraphIgnoreAttribute>()) != null;
        //            if (!ignore && !primitivity.HasFlag(GraphPrimitivity.Function) && primitivity.HasFlag(GraphPrimitivity.Object) && !serializable)
        //                JoinRoller(prop, new Clause { }, childJoiner =>
        //                {
        //                    var trueType = ObjectHelper.TrueType(prop.PropertyType);
        //                    if (childJoiner.JoinRegistry.Add($"{prop.DeclaringType?.Namespace}.{prop.Name}"))
        //                    {
        //                        childJoiner.RootVar.Type = trueType;
        //                        childJoiner.JoinAllChildrenRecursive(depth - 1);
        //                    }
        //                    else
        //                    {
        //                        Debug.WriteLine("Duplicate filtered");
        //                    }
        //                }, true);
        //        }
        //    }
        //    return this;
        //}
    }
}
