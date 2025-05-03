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
    public class Neo4jJoiner : IJoiner
    {
        public CypherVar RootVar { get; protected set; } = new();

        private Neo4jJoiner JoinRoller<T, P>(Expression<Func<T, P>> operand, Clause clause, Action<IJoiner> deepJoiner, bool optional) 
            => JoinRoller(operand.GetExpressionProperty(), clause, deepJoiner, optional);
        private Neo4jJoiner JoinRoller(PropertyInfo member, Clause clause, Action<IJoiner> deepJoiner, bool optional)
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
                        deepJoiner(enumFieldSubfactory);
                        RootVar.Connections[$"{member.Name}.{enumFieldLabel}"] = enumFieldSubfactory.RootVar;
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
            deepJoiner(subfactory);
            RootVar.Connections[member.Name] = subfactory.RootVar;

            return this;
        }

        public IJoiner Join<T, P>(Expression<Func<T, P?>> operand) => Join(operand, new Clause(), _ => { });
        public IJoiner Join<T, P>(Expression<Func<T, P>> operand, Clause clause) => Join(operand, clause, _ => { });
        public IJoiner Join<T, P>(Expression<Func<T, P>> operand, Action<IJoiner> deepJoiner) => Join(operand, new Clause(), deepJoiner);
        public IJoiner Join<T, P>(Expression<Func<T, P>> operand, Clause clause, Action<IJoiner> deepJoiner) => JoinRoller(operand, clause, deepJoiner, false);
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand) => JoinOptional(operand, new Clause(), _ => { });
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Clause clause) => JoinOptional(operand, clause, _ => { });
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Action<IJoiner> deepJoiner) => JoinOptional(operand, new Clause(), deepJoiner);
        public IJoiner JoinOptional<T, P>(Expression<Func<T, P>> operand, Clause clause, Action<IJoiner> deepJoiner) => JoinRoller(operand, clause, deepJoiner, true);
        public IJoiner JoinAllChildren() => JoinAllChildrenRecursive(1);
        public IJoiner JoinAllChildrenRecursive(int depth)
        {
            if (depth > 0)
            {
                foreach(var prop in RootVar.Type?.GetProperties() ?? [])
                {
                    var primitivity = ObjectHelper.GetPrimitivity(prop.PropertyType);
                    var serializable = ObjectHelper.IsSerializable(prop);
                    var ignore = (prop.GetCustomAttribute<GraphIgnoreAttribute>() ?? prop.PropertyType.GetCustomAttribute<GraphIgnoreAttribute>()) != null;
                    if (primitivity.HasFlag(GraphPrimitivity.Function))
                        Debug.WriteLine($"{RootVar.Type?.Name}.{prop.Name} is a funcable");
                    if(!ignore && !primitivity.HasFlag(GraphPrimitivity.Function) && primitivity.HasFlag(GraphPrimitivity.Object) && !serializable)
                        JoinRoller(prop, new Clause { }, j => j.JoinAllChildrenRecursive(depth - 1), true);
                }
            }
            return this;
        }
    }
}
