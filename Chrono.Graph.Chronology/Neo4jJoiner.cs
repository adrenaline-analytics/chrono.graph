using Chrono.Graph.Core.Application;
using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Domain;
using Chrono.Graph.Core.Notations;
using Chrono.Graph.Core.Utilities;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

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
                if ((dicInfo?.KeyType?.IsEnum  ?? false) && member.GetCustomAttribute<GraphKeyLabellingAttribute>() != null)
                {
                    foreach(var enumFieldLabel in ObjectHelper.GetDictionaryLabels(dicInfo, member))
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
            deepJoiner(subfactory);
            RootVar.Connections[member.Name] = subfactory.RootVar;

            return this;
        }

        public IJoiner Join<T, P>(Expression<Func<T, P?>> operand) => Join(operand, _ => { }, _ => { });
        public IJoiner Join<T, P>(Expression<Func<T, P>> operand, Action<IQueryClause> clause) => Join(operand, clause, _ => { });
        public IJoiner Join<T, P>(Expression<Func<T, P>> operand, Action<IJoiner> deepJoiner) => Join(operand, _ => { }, deepJoiner);
        public IJoiner Join<T, P>(Expression<Func<T, P>> operand, Action<IQueryClause> clause, Action<IJoiner> deepJoiner) => JoinRoller(operand, clause, deepJoiner, false);
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand) => JoinOptional(operand, _ => { }, _ => { });
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Action<IQueryClause> clause) => JoinOptional(operand, clause, _ => { });
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Action<IJoiner> deepJoiner) => JoinOptional(operand, _ => { }, deepJoiner);
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Action<IQueryClause> clause, Action<IJoiner> deepJoiner) => JoinRoller(operand, clause, deepJoiner, true);
    }
}
