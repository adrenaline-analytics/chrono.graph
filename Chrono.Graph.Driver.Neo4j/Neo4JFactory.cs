using Castle.Core.Internal;
using Chrono.Graph.Core.Application;
using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Domain;
using Chrono.Graph.Core.Notations;
using Chrono.Graph.Core.Utilities;
using System.Linq.Expressions;
using System.Reflection;

namespace Chrono.Graph.Adapter.Neo4j
{

    public class Neo4jFactory : Neo4jJoiner, IQueryFactory
    {
        private Dictionary<string, ISubQueryFactory> _subFactories = new Dictionary<string, ISubQueryFactory>();

        public string Hash { get; private set; }
        public bool Locked { get; private set; }

        public Statement Statement { get; set; }
        public Dictionary<string, Clause> Clauses { get; set; } = new Dictionary<string, Clause>();
        public IEnumerable<ClauseGroup> SubClauses { get; set; } = new List<ClauseGroup>();
        public Dictionary<string, HashSet<GraphEdgeBasic>> InboundEdges { get; set; } = new Dictionary<string, HashSet<GraphEdgeBasic>>();
        public Dictionary<int, IQueryFactory> GlobalObjectRegistry { get; } = [];

        private Neo4jFactory(Statement statement, int hash, Dictionary<int, IQueryFactory> objectRegistry)
        {
            Statement = statement;
            GlobalObjectRegistry = objectRegistry;
            if (GlobalObjectRegistry.TryGetValue(hash, out var currentFactory))
                MergeFactory(this, currentFactory);

            GlobalObjectRegistry[hash] = this;
            Hash = hash.ToString("X8")
                .Substring(0, 8)
                .Replace("-", "d")
                .Replace("_", "u");
        }

        private static IQueryFactory ConnectChildWithCreate<T>(T thing, PropertyInfo? prop, Dictionary<int, IQueryFactory> registry)
            => thing != null && registry.TryGetValue(thing.GetHashCode(), out var factory)
                ? factory
                : BootstrapWithCreate(thing, prop, registry);
        private static IQueryFactory ConnectChildWithMatch<T>(T thing, Action<IQueryClause> clauser, Dictionary<int, IQueryFactory> registry)
        {
            if (thing == null)
                throw new DataMisalignedException("Cannot determine object type");

                return BootstrapStatement(CypherConstants.MatchCommand, clauser, thing.GetType(), registry, thing.GetHashCode());
        }
        private static IQueryFactory ConnectChildWithMerge<T>(T thing, Action<IQueryClause> clauser, Dictionary<int, IQueryFactory> registry)
        {
            if (thing == null)
                throw new DataMisalignedException("Cannot determine object type");

            return registry.TryGetValue(thing.GetHashCode(), out var factory)
                ? factory
                : BootstrapStatement(CypherConstants.MergeCommand, clauser, thing.GetType(), registry, thing.GetHashCode());
        }
        private static IQueryFactory BootstrapStatement<T>(string command, Action<IQueryClause> clauser, int? hash = null) => BootstrapStatement(command, clauser, typeof(T), hash);
        private static IQueryFactory BootstrapStatement(string command, Action<IQueryClause> clauser, Type type, int? hash = null) => BootstrapStatement(command, clauser, type, [], hash);
        private static IQueryFactory BootstrapStatement(string command, Action<IQueryClause> clauser, Type type, Dictionary<int, IQueryFactory> registry, int? hash = null)
        {
            hash = hash ?? new Random().Next(1, 2000000000);
            var statement = new Statement();

            var factory = new Neo4jFactory(statement, hash.Value, registry);
            clauser(factory);
            var label = ObjectHelper.GetObjectLabel(type);
            factory.RootVar = new CypherVar
            {
                Hash = factory.Hash,
                Label = label,
                GraphType = GraphObjectType.Node,
                Type = type,
            };
            command += $" {factory.GenerateMatchishStatement(type)}";
            statement.Commands = [.. statement.Commands, command];
            return factory;
        }
        private static IQueryFactory BootstrapWithCreate<T>(T thing, PropertyInfo? prop, Dictionary<int, IQueryFactory> registry)
        {
            var statement = new Statement();

            if (thing == null)
                throw new DataMisalignedException("Cannot determine object type");

            var hash = thing.GetHashCode();
            var factory = new Neo4jFactory(statement, hash, registry);
            var vars = new Dictionary<string, object>();
            var idProp = ObjectHelper.GetIdProp(thing.GetType());
            var idValue = idProp.GetValue(thing);
            if (idValue != null)
                vars.Add(ObjectHelper.GetPropertyLabel(idProp), idValue);
            vars.Merge(factory.GeneratePropertiesDict(thing));
            var prefix = "create";

            //add match clauses even on create
            //on match set {} //do nothing
            //on create

            var label = prop == null
                ? ObjectHelper.GetObjectLabel(thing.GetType())
                : ObjectHelper.GetPropertyLabel(prop);

            factory.RootVar = new CypherVar
            {
                Hash = factory.Hash,
                Object = thing,
                Type = thing.GetType(),
                Label = label,
                GraphType = GraphObjectType.Node
            };

            var makeKey = new Func<string, string>(s => $"{prefix}{s}{factory.Hash}");

            statement.InVars.Merge(vars.ToDictionary(
                i => makeKey(i.Key),
                i => new CypherVar {
                    Object = i.Value,
                    Var = makeKey(i.Key)
                }));

            statement.OutVars.Add(factory.RootVar.Var,
                new CypherVar
                {
                    GraphType = GraphObjectType.Node,
                    Label = label,
                    Var = factory.RootVar.Var
                });
            var properties = vars.Any()
                ? $" {{{vars.Select(i => $"{i.Key}:${prefix}{i.Key}{factory.Hash}").Aggregate((a, b) => $"{a}, {b}")}}}"
                : "";
            statement.Commands = [.. statement.Commands, $"CREATE ({factory.RootVar.Var}: {label}{properties})"];

            return factory;

        }

        internal static IQueryFactory BootstrapWithCreate<T>(T thing) => BootstrapWithCreate(thing, null);
        internal static IQueryFactory BootstrapWithCreate<T>(T thing, PropertyInfo? prop) => BootstrapWithCreate(thing, prop, []);

        internal static IQueryFactory BootstrapWithMatch<T>(Action<IQueryClause> clauser) => BootstrapStatement<T>(CypherConstants.MatchCommand, clauser);
        internal static IQueryFactory BootstrapWithMatch<T>(T thing, Action<IQueryClause> clauser)
        {
            if (thing == null)
                throw new DataMisalignedException("Cannot determine object type");
            return BootstrapStatement(CypherConstants.MatchCommand, clauser, thing.GetType(), thing?.GetHashCode());
        }
        internal static IQueryFactory BootstrapWithMerge<T>(T thing, Action<IQueryClause> clauser)
        {
            if (thing == null)
                throw new DataMisalignedException("Cannot determine object type");
            return BootstrapStatement(CypherConstants.MergeCommand, clauser, thing.GetType(), thing.GetHashCode());
        }
        internal static void MergeFactory(IQueryFactory me, IQueryFactory them)
        {
            if (them.Statement != null)
                me.Statement += them.Statement;
            if (them.Clauses?.Any() ?? false)
                me.Clauses.Merge(them.Clauses);
            if (them.SubClauses?.Any() ?? false)
                me.SubClauses = them.SubClauses.Union(them.SubClauses);
            if (them.InboundEdges?.Any() ?? false)
                me.InboundEdges.Merge(them.InboundEdges);
        }


        internal void GenerateOnAnythingStatement(object thing, string onWhat, bool idempotent)
        {
            var injectables = GeneratePropertiesDict(thing);
            if (!injectables.Any())
                return;

            var prefix = onWhat.ToLower();
            var outVar = Statement.OutVars.FirstOrDefault().Value.Var;

            Statement.DoOns[onWhat] = $"ON {onWhat} SET\n\t{injectables.Select(i => $"{outVar}.{i.Key}=${prefix}{i.Key}{Hash}").Aggregate((a, b) => $"{a},\n\t{b}")}";
            if (idempotent && onWhat == CypherConstants.MatchCommand)
            {
                var deletables = GeneratePropertyNullsDict(thing);
                if (deletables.Any())
                    Statement.DoOns[CypherConstants.RemoveCommand] = $"{CypherConstants.RemoveCommand}\n\t{deletables.Select(i => $"{RootVar.Var}.{i}").Aggregate((a, b) => $"{a},\n\t{b}")}";
            }

            var makeKey = new Func<string, string>(s => $"{prefix}{s}{Hash}");
            Statement.InVars.Merge(injectables.ToDictionary(i => makeKey(i.Key), i => new CypherVar { Object = i.Value, Var = makeKey(i.Key) }));

        }
        internal Dictionary<string, Clause> RecurseSubClausesForVars(IEnumerable<ClauseGroup>? clauseGroups)
        {
            var result = new Dictionary<string, Clause>();
            foreach(var clauseGroup in (clauseGroups ?? []).Where(c => c.Clauses?.Any() ?? false))
            {
                result.Merge(clauseGroup.Clauses);
                var subVars = RecurseSubClausesForVars(clauseGroup.SubClauses);
                if (subVars.Any())
                    result.Merge(subVars);
            }
            return result;
        }
        internal string RecurseSubClausesForQuery(IEnumerable<ClauseGroup>? clauseGroups)
        {
            var result = new List<string>();
            foreach(var clauseGroup in (clauseGroups ?? []).Where(c => c.Clauses?.Any() ?? false))
            {
                var values = clauseGroup.Clauses
                        .Select(c => $"{c.Key}{c.Value.Operator} ${c.Key}{Hash}")
                        .Aggregate((a, b) => $"{a}, {b}");
                var subValues = RecurseSubClausesForQuery(clauseGroup.SubClauses);
                if(!string.IsNullOrEmpty(subValues))
                    values = $"{values}, {subValues}";

                result.Add(values);
            }
            return result.Any() ? result.Aggregate((a, b) => $"{a}, {b}") : "";
        }
        internal string GenerateMatchishStatement(Type type)
        {
            var matchClause = Clauses
                .Select(c => $"{c.Key}{c.Value.Operator} ${c.Key}{Hash}")
                .Aggregate((a, b) => $"{a}, {b}");

            var makeKey = new Func<string, string>(s => $"{s}{Hash}");

            Statement.InVars.Merge(Clauses.ToDictionary(c => makeKey(c.Key), c =>
                new CypherVar {
                    Object = c.Value.Operand ?? new object(),
                    Var = makeKey(c.Key)
                }));

            if (SubClauses.Any(c => c.Clauses?.Any() ?? false))
            {
                var subMatchClause = RecurseSubClausesForQuery(SubClauses);
                var subVars = RecurseSubClausesForVars(SubClauses);
                Statement.InVars.Merge(subVars.ToDictionary(c => makeKey(c.Key), c =>
                    new CypherVar {
                        Object = c.Value.Operand ?? new object(),
                        Var = makeKey(c.Key)
                    }));

                matchClause = $"{matchClause}, {subMatchClause}";
            }


            var label = Utils.StandardizeNodeLabel(ObjectHelper.GetObjectLabel(type));
            var outVar = new CypherVar
            {
                GraphType = GraphObjectType.Node,
                Label = label,
                Var = RootVar.Var
            };

            if (!Statement.OutVars.TryAdd(RootVar.Var, outVar)
                && Statement.OutVars.TryGetValue(RootVar.Var, out var existingOutVar)
                && (!existingOutVar.Equals(outVar)))
                new DataMisalignedException($"Cypher object [{RootVar.Var}:{RootVar.Label}] is non idempotent. A data collision has occured when assigning a variable name [{RootVar.Var}] to this graph object, this object has already been asigned but with a different value.");

            return $"({RootVar.Var}:{label} {{{matchClause}}})";
        }
        internal string[] GeneratePropertyNullsDict(object thing)
        {
            var type = thing.GetType();
            var idProp = ObjectHelper.GetIdProp(type);
            return type.GetProperties()
                .Where(p =>  p.GetAttribute<GraphIgnoreAttribute>() == null && p.GetValue(thing) == null)
                .Select(p => $"{p.Name}").ToArray();
        }
        internal Dictionary<string, object> GeneratePropertiesDict(object thing)
        {
            var type = thing.GetType();
            var idProp = ObjectHelper.GetIdProp(type);
            return type.GetProperties()
                .Where(p =>
                    p.GetAttribute<GraphIgnoreAttribute>() == null
                    && p.GetValue(thing) != null
                    && !p.Equals(idProp)
                    && ObjectHelper.IsSerializable(p))
                .ToDictionary(
                    x => Utils.StandardizePropertyName(ObjectHelper.GetPropertyLabel(x)),
                    x => x.GetValue(thing) ?? new object());
        }

        internal string GenerateVarString(CypherVar cypherVar) =>
            cypherVar.Connections.Any(c => c.Value?.Edge != null)
                ? cypherVar.Connections
                    .Select(c => $"{(c.Value?.Edge?.Optional ?? false ? CypherConstants.OptionalMatchCommand : CypherConstants.MatchCommand)} ({cypherVar.Var})-[{c.Value?.Edge?.Var ?? throw new DataMisalignedException("Variable not set")}{(!string.IsNullOrEmpty(c.Value.Edge?.Label) ? $":{c.Value.Edge.Label}" : "")}]-({c.Value.Var})")
                    .Aggregate((a, b) => $"{a}\n{b}")
                : string.Empty;


        public IQueryFactory Command<T>(T connectTo, Action<IQueryFactory> builder, IQueryFactory factory)
        {
            if (!_subFactories.TryAdd(factory.Hash, factory))
                return (Neo4jFactory)_subFactories[factory.Hash];

            builder(factory);
            return factory;
        }
        internal void ConnectEdges(IQueryFactory factoryA, IQueryFactory factoryB, Func<GraphEdgeDetails> edgeDefiner)
        {
            var edge = edgeDefiner();

            //make sure to use the opposite factory hash.
            var (hash, edges) = edge.Direction == GraphEdgeDirection.In
                ? (factoryB.RootVar.Var, factoryA.InboundEdges)
                : (factoryA.RootVar.Var, factoryB.InboundEdges);

            if (string.IsNullOrEmpty(edge.Label))
                throw new ArgumentException("An edge label is required to make a graph edge connection");

            if (!edges.TryAdd(hash, [edge]) && edges.TryGetValue(hash, out var existingEdges))
                existingEdges.Add(edge);
        }




        public Statement? Build()
        {
            if (Locked)
                return null;

            Locked = true;

            foreach (var kvp in Statement.InVars)
                Statement.InVars[kvp.Key].Object = Utils.StandardizePropertyValue(kvp.Value.Object);

            var edgeAction = Statement.Commands.Any(c => c.StartsWith(CypherConstants.CreateCommand))
                ? "CREATE"
                : "MERGE";

            var comandStack = Statement.Commands.Any()
                ? Statement.Commands.Aggregate((a, b) => $"{a}\n{b}")
                : throw new NotImplementedException("A commandless command is not yet implemented");

            var actionAggregate = new Func<string, string[], string>((prefix, items) =>
                items.Any(i => !string.IsNullOrEmpty(i))
                    ? $"{prefix} {items.Where(i => !string.IsNullOrEmpty(i)).Aggregate((a, b) => $"{a}, {b}")}"
                    : string.Empty);

            var returns = actionAggregate(CypherConstants.ReturnCommand, Statement.Returns);
            var deletes = actionAggregate(CypherConstants.DeleteCommand, Statement.Deletes);
            var doOns = Statement.DoOns.Any(d => !string.IsNullOrEmpty(d.Key) && !string.IsNullOrEmpty(d.Value))
                ? Statement.DoOns.Where(d => !string.IsNullOrEmpty(d.Key) && !string.IsNullOrEmpty(d.Value))
                    .Select(d => d.Value)
                    .Aggregate((a, b) => $"{a}\n{b}")
                : string.Empty;

            var actionSet = new string[]
            {
                returns,
                deletes,
                doOns
            };

            var actions = actionSet.Any(a => !string.IsNullOrEmpty(a))
                ? $"\n{actionSet.Where(a => !string.IsNullOrEmpty(a)).Aggregate((a, b) => $"{a}\n{b}")}"
                : string.Empty;


            Statement.NodeCypher = $"{comandStack} {Statement.Query}{actions}";

            var subStatements = _subFactories
                .Select(subFactory => ((Neo4jFactory)subFactory.Value).Build())
                .Where(subStatement => subStatement != null)
                .ToList(); //Enumeration is important here for Build() to occur before the next line in each subfactory

            if (subStatements.Any())
            {

                Statement.InVars.Merge(subStatements
                    .SelectMany(s => s.InVars
                        .Select(v => v))
                    .GroupBy(s => s.Key)
                    .ToDictionary(s => s.Key, s => s.Last().Value));

                var subNodes = subStatements
                    .Select(s => s.NodeCypher)
                    .Aggregate((a, b) => $"{a}\n{b}");

                Statement.NodeCypher = !string.IsNullOrEmpty(subNodes)
                    ? $"{Statement.NodeCypher}\n{subNodes}"
                    : Statement.NodeCypher;

                //null/empty checking is down stairs
                Statement.EdgeCypher = subStatements
                    .Select(s => s.EdgeCypher)
                    .Aggregate((a, b) => $"{a}\n{b}");

            }

            var edges = string.Empty;
            if (InboundEdges.Any())
            {
                edges = InboundEdges
                    .SelectMany(d => d.Value
                        .Select((edge, index) => {
                            var edgeVar = $"edge{Hash}{d.Key}{index}";
                            var edgeLabel = Utils.StandardizeEdgeLabel(edge.Label);
                            var edgeProperties = edge.Properties.Any()
                                ? $" {{{edge.Properties.Select(p => $"{p.Key}:\"{p.Value}\"").Aggregate((a, b) => $"{a},{b}")}}}"
                                : "";

                            return $"{edgeAction} ({d.Key})-[{edgeVar}:{edgeLabel}{edgeProperties}]->({RootVar.Var})";

                        }))
                    .Aggregate((a, b) => $"{a}\n{b}");
            }

            Statement.EdgeCypher = !string.IsNullOrEmpty(edges)
                ? !string.IsNullOrEmpty(Statement.EdgeCypher)
                    ? $"{Statement.EdgeCypher}\n{edges}" //dont want the \n if EdgeCypher is empty
                    : edges
                : Statement.EdgeCypher ?? string.Empty;


            Statement.Cypher = !string.IsNullOrEmpty(Statement.NodeCypher)
                || !string.IsNullOrEmpty(Statement.EdgeCypher)
                    ? new string[]
                        {
                            Statement.NodeCypher,
                            Statement.EdgeCypher,
                        }
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Aggregate((a, b) => $"{a}\n{b}")
                    : string.Empty;

            return Statement;
        }

        public void Create(object? thing)
        {
            var dict = GeneratePropertiesDict(thing);
            var prefix = "create";
            var label = ObjectHelper.GetObjectLabel(thing.GetType());
            var makeKey = new Func<string, string>(s => $"{prefix}{s}{Hash}");

            Statement.InVars.Merge(dict.ToDictionary(
                i => makeKey(i.Key),
                i => new CypherVar
                {
                    Object = i.Value,
                    Var = makeKey(i.Key),
                }));

            Statement.OutVars.Add(RootVar.Var,
                new CypherVar
                {
                    GraphType = GraphObjectType.Node,
                    Label = label,
                    Var = RootVar.Var,
                });

            Statement.Commands = [.. Statement.Commands, $"{CypherConstants.CreateCommand} ({RootVar.Var}: {label} {{{dict.Select(i => $"{i.Key}:${prefix}{i.Key}{Hash}").Aggregate((a, b) => $"{a}, {b}")}}})"];
        }

        public void CreateChild(object? parent, object? child, PropertyInfo? prop) => CreateChild(parent, child, prop, f => { });
        public void CreateChild(object? parent, object? child, PropertyInfo? prop, Action<ISubQueryFactory> builder)
            => CreateChild(parent, child, prop, builder, prop?.Name ?? child.GetType().Name);
        public void CreateChild(object? parent, object? child, PropertyInfo? prop, Action<ISubQueryFactory> builder, string label)
            => CreateChild(parent, child, prop, builder, () => ObjectHelper.GetPropertyEdge(prop, label: label));
        public void CreateChild(object? parent, object? child, PropertyInfo? prop, Action<ISubQueryFactory> builder, Func<GraphEdgeDetails> edgeDefiner)
        {
            if (child == null || parent == null)
                return;

            if (!GlobalObjectRegistry.TryGetValue(parent.GetHashCode(), out var parentFactory))
                throw new DataMisalignedException($"Cannot find parent factory for {parent.GetHashCode()} to connect to child {child.GetHashCode()}");

            var childType = child.GetType();
            var idProp = ObjectHelper.GetIdProp(child.GetType());
            var idValue = idProp.GetValue(child);

            //if id found connect with match
            //if no id, connect with create
            var matchable = idValue != null;
            var subFactory = matchable
                ? Command(child, f => { }, ConnectChildWithMatch(child, q => q.Where(idProp.Name, Is.Equal(idValue), childType), GlobalObjectRegistry))
                : Command(child, f => { }, ConnectChildWithCreate(child, prop, GlobalObjectRegistry));

            if(matchable)
                subFactory.OnCreateSet(child);

            builder(subFactory);
            ConnectEdges(parentFactory, subFactory, edgeDefiner);
        }
        public void Delete() => Delete(Statement.OutVars.FirstOrDefault().Value);
        public void Delete(CypherVar outVar) => Statement.Deletes = [.. Statement.Deletes, outVar.Var];

        public void HotLink<TT>(string verb, TT to)
        {
            var idProp = ObjectHelper.GetIdProp(to.GetType());
            HotLink(verb, to, c => c.Where(idProp.Name, Is.Equal(idProp.GetValue(to)), to.GetType()));
        }
        public void HotLink<TT>(string verb, TT to, Action<IQueryClause> clause)
        {

        }
        public void Match<T>(T thing, Action<IQueryClause> clauser, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void Match<T>(T thing, string label, Action<IQueryClause> clauser, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void Match<T>(T thing, PropertyInfo property, Action<IQueryClause> clauser, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void Match(CypherVar cypherVar) => Match(new List<CypherVar>() { cypherVar });
        public void Match(IEnumerable<CypherVar> cypherVars)
        {
            foreach (var v in cypherVars)
            {
                var varString = GenerateVarString(v);

                if (!string.IsNullOrEmpty(varString))
                    Statement.Commands = [.. Statement.Commands, varString];

                if (v.Connections.Any())
                    Match(v.Connections.Select(c => c.Value));
            }
        }

        public void Merge<T>(T thing, Action<IQueryClause> clauser, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void Merge<T>(T thing, string label, Action<IQueryClause> clauser, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void Merge<T>(T thing, PropertyInfo property, Action<IQueryClause> clauser, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void MatchChild<A, B>(A from, B connectTo, Action<IQueryClause> clauser, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void MatchChild<A, B>(A from, B connectTo, PropertyInfo property, Action<IQueryClause> clauser, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void MatchChild<A, B>(A parent, B child, Action<IQueryClause> clausation, Action<ISubQueryFactory> build, Func<GraphEdgeDetails> edgeDefiner) => throw new NotImplementedException();
        //{
            //if (child == null || parent == null)
            //    return;

            //if (!GlobalObjectRegistry.TryGetValue(parent.GetHashCode(), out var parentFactory))
            //    throw new DataMisalignedException($"Cannot find parent factory for {parent.GetHashCode()} to connect to child {child.GetHashCode()}");

            //var childType = child.GetType();
            //var idProp = ObjectInfo.GetIdProp(child.GetType());
            //var idValue = idProp.GetValue(child);

            //if (idValue == null)
            //    throw new ArgumentException($"Cannot determine id value for object type {childType.Name}");

            //var subFactory = Command(child, f => { }, ConnectChildWithMatch(child, q => q.Where(idProp.Name, Is.Equal(idValue), childType), GlobalObjectRegistry));

            //build(subFactory);
            //ConnectEdges(parentFactory, subFactory, edgeDefiner);
        //}
        public void MergeChild<A, B>(A parent, B child, Action<IQueryClause> clauser, Action<ISubQueryFactory> build) => throw new NotImplementedException();
        public void MergeChild<A, B>(A parent, B child, PropertyInfo connectedProperty, Action<IQueryClause> clauser, Action<ISubQueryFactory> builder)
            => MergeChild(parent, child, clauser, builder,
                () => {
                        var attr = connectedProperty.GetCustomAttribute<GraphEdgeAttribute>()?.Definition
                            ?? connectedProperty.GetType().GetCustomAttribute<GraphEdgeAttribute>()?.Definition;
                        return new GraphEdgeDetails
                        {
                            Label = !string.IsNullOrEmpty(attr?.Label ?? "") ? attr?.Label ?? connectedProperty.Name : connectedProperty.Name,
                            Direction = attr != null ? attr.Direction : GraphEdgeDirection.Out
                        };
                    });
        public void MergeChild<A, B>(A parent, B child, Action<IQueryClause> clauser, Action<ISubQueryFactory> builder, Func<GraphEdgeDetails> edgeDefiner)
        {
            if(GlobalObjectRegistry.TryGetValue(parent.GetHashCode(), out var parentFactory))
            {
                if (parent.Equals(child))
                    throw new ArgumentException("Parent connecting to itself, not sure this is okay yet");
                var childFactory = ConnectChildWithMerge(child, clauser, GlobalObjectRegistry);
                ConnectEdges(parentFactory, parentFactory.Command(child, builder, childFactory), edgeDefiner);

            }
        }

        public void OnCreateSet<T>(T thing) => GenerateOnAnythingStatement(thing, CypherConstants.CreateCommand, false);
        public void OnMatchSet<T>(T thing, bool idempotent) => GenerateOnAnythingStatement(thing, CypherConstants.MatchCommand, idempotent);

        public void OptionalMatch<T>(T thing, bool idempotent) => throw new NotImplementedException();

        public void Return() => Return(RootVar);
        public void Return(CypherVar outVar) => Statement.Returns = [.. Statement.Returns, outVar.Var];
        public void Return(IEnumerable<CypherVar> outVars) => Return(outVars.Select(o => o.Var));
        public void Return(IEnumerable<string> outVars) => Statement.Returns = [.. Statement.Returns, outVars.Aggregate((a, b) => $"{a}, {b}")];
        public void Return(Action<IJoiner> joiner)
        {
            var outVars = new Queue<CypherVar>();
            var connectedVars = new Queue<string>();

            joiner(this);

            RootVar.Connections.Recurse(outVars.Enqueue);
            RootVar.Connections.Recurse(t => {
                var primitivity = ObjectHelper.GetPrimitivity(t.Type);
                connectedVars.Enqueue(primitivity.HasFlag(GraphPrimitivity.Array) ? $"COLLECT({t.Var}) AS {t.Var}" : t.Var);
                if (!string.IsNullOrEmpty(t.Edge?.Var))
                    connectedVars.Enqueue(primitivity.HasFlag(GraphPrimitivity.Array) ? $"COLLECT({t.Edge.Var}) AS {t.Edge.Var}" : t.Edge.Var);
            });

            Statement.OutVars.Merge(outVars.ToDictionary(v => v.Var, v => v));
            Match(RootVar);
            Return(RootVar);
            Return(connectedVars);
        }

        public IQueryClauseGroup Where<T, P>(Expression<Func<T, P?>> operand, Clause clause) => Where<T>(operand.GetExpressionPropertyName(), clause);
        public IQueryClauseGroup Where<T>(string operand, Clause clause) => Where(operand, clause, typeof(T));
        public IQueryClauseGroup Where(string propertyName, Clause clause, Type type)
        {
            if (!Clauses.TryAdd(ObjectHelper.GetPropertyLabel(type, propertyName), clause)
                && Clauses.TryGetValue(ObjectHelper.GetPropertyLabel(type, propertyName), out var existingClause)
                && !existingClause.Equals(clause))
                throw new ArgumentException("A data collision has occurred when attempting to build a clause");

            var subclause = new ClauseGroup();
            SubClauses = SubClauses.Append(subclause);
            return subclause;
        }

        public IQueryClauseGroup WhereGroup(Action<IQueryFactory> builder)
        {
            return new ClauseGroup();
        }

    }
}
