using Chrono.Graph.Core.Application;
using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Domain;
using Chrono.Graph.Core.Notations;
using Chrono.Graph.Core.Utilities;
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
            var primitivity = ObjectHelper.GetPrimitivity(member.PropertyType);
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
                            Var = $"{enumFieldLabel}{Utils.CypherId()}",
                            Edge = ObjectHelper.GetPropertyEdge(member, true, enumFieldLabel), //must always be OPTIONAL MATCH
                            Label = enumFieldLabel,
                        };
                        deepJoiner(enumFieldSubfactory);
                        RootVar.Connections[$"{member.Name}.{enumFieldLabel}"] = enumFieldSubfactory.RootVar;
                    }
                    return this;

                }

            }
            var subfactory = new Neo4jJoiner();
            var label = ObjectHelper.GetPropertyLabel(member);
            subfactory.RootVar = new CypherVar
            {
                Type = member.PropertyType,
                Var = $"{label}{Utils.CypherId()}",
                Edge = ObjectHelper.GetPropertyEdge(member, optional),
                Label = label,
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
                    if(primitivity.HasFlag(GraphPrimitivity.Object) && !serializable)
                        JoinRoller(prop, new Clause { }, j => j.JoinAllChildrenRecursive(depth - 1), true);
                }
            }
            return this;
        }
    }
}
