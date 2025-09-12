using System.Reflection;
using Castle.Core.Internal;
using Chrono.Graph.Core.Application;
using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Domain;
using Chrono.Graph.Core.Notations;
using Chrono.Graph.Core.Utilities;
using Neo4j.Driver;


namespace Chrono.Graph.Adapter.Neo4j
{
    public class Neo4jDatabase : IGraphDatabase
    {
        private readonly IDriver _driver;
        private readonly QueryConfig _queryConfig;

        public Neo4jDatabase(IDriver driver)
        {
            _driver = driver;
            _queryConfig = new QueryConfig(database: Environment.GetEnvironmentVariable("NEO4J_DATABASE") ?? "chrono");
        }

        public Task<T?> GetScalar<T>(Action<IQueryClause> clauser) where T : class => GetScalar<T>(clauser, null);
        public async Task<T?> GetScalar<T>(Action<IQueryClause> clauser, Action<IJoiner>? joiner) where T : class
        {
            var result = (await Get<T>(clauser, joiner)).ToList();
            return result.Count > 0 ? result[0] : null;
        }
        public Task<IEnumerable<T>> Get<T>(Action<IQueryClause> clauser) where T : class => Get<T>(clauser, null);
        public async Task<IEnumerable<T>> Get<T>(Action<IQueryClause> clauser, Action<IJoiner>? joiner) where T : class
        {
            var rootNodeName = "node";
            IQueryFactory? factory = null;
            var transaction = new CypherTransaction(_driver, _queryConfig);
            var (queryResult, summary) = await transaction.Execute(() =>
            {
                factory = Neo4jFactory.BootstrapWithMatch<T>(clauser);
                if (joiner != null)
                {
                    //factory.Return(joiner);
                    factory.ReturnStructured(joiner);
                }
                else
                {
                    factory.Return();
                }
                rootNodeName += factory.Hash;
                return factory;

            });//$"{query.BootstrapWithMatch()} {query.BuildReturnStatement()}");

            if (factory == null)
                return [];

            var result = queryResult.Count > 0 ? queryResult.Select(record =>
            {
                T instance = ObjectHelper.Instantiate<T>();
                new Hydrator2000().Hydrate(instance, record, factory.RootVar);
                return instance;
            }).Where(i => i != null).ToList() : [];
            return ((result?.Count ?? 0) > 0) ? result ?? [] : [];
        }
        public Task Delete<T>(Action<IQueryClause> clauser, Action<IJoiner> joiner) where T : class => throw new NotImplementedException();
        public Task Delete<T>(T thing, int nodeDepth) where T : class => throw new NotImplementedException();
        public async Task Delete<T>(T thing) where T : class => await Delete(thing, ObjectHelper.GetIdProp<T>());
        public async Task Delete<T>(T thing, PropertyInfo idProp) where T : class
        {
            var val = idProp.GetValue(thing) ?? throw new AmbiguousMatchException("An object was attempting to be deleted without an identifier.  Why come it's unscannable?");
            await Delete<T>(q => q.Where<T>(idProp.Name, Is.Equal(val)));

        }
        public async Task Delete<T>(Action<IQueryClause> clauser) where T : class
            => await new CypherTransaction(_driver, _queryConfig)
                .Execute(() =>
                {
                    var factory = Neo4jFactory.BootstrapWithMatch<T>(clauser);
                    factory.Delete();
                    return factory;
                });


        public Task RemoveEdge<T, TT>(T from, string edge, TT what) where T : class
        {
            throw new NotImplementedException();

        }
        public Task AddEdge<T, TT>(T from, string verb, TT to) where T : class
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Does NOT UPDATE child objects that already exist, 
        /// BUT Does ADD new child objects to child objects that already exist if they aren't added already
        /// Creates object and any child objects that do not exist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <returns></returns>
        public Task Post<T>(T thing) where T : class => Post(thing, j => { });
        public async Task Post<T>(T thing, Action<IJoiner> joiner) where T : class
        {
            var transaction = new CypherTransaction(_driver, _queryConfig, () =>
            {

                var idProp = ObjectHelper.GetIdProp(typeof(T));
                var factory = Neo4jFactory.BootstrapWithMerge<T>(thing, c => c.Where(idProp.Name, Is.Equal(idProp.GetValue(thing)), thing.GetType()));
                factory.OnCreateSet(thing);
                return factory;
            });
            if (transaction.Factory == null)
                throw new DatabaseException("Unable to generate cypher factory");

            joiner(transaction.Factory);
            PostChildren(thing, transaction, transaction.Factory);
            await transaction.Execute();
        }

        public Task Put<T>(T thing) where T : class => Put(thing, ObjectHelper.GetIdProp(thing.GetType()), j => { });
        public Task Put<T>(T thing, Action<IJoiner> joiner) where T : class => Put(thing, ObjectHelper.GetIdProp(thing.GetType()), joiner);
        public Task Put<T>(T thing, PropertyInfo idProp, Action<IJoiner> joiner) where T : class
        {
            var val = idProp.GetValue(thing) ?? throw new AmbiguousMatchException("An object was attempting to be saved without an identifier.  Why come it's unscannable?");
            return Put(thing, q => q.Where<T>(idProp.Name, Is.Equal(val)), joiner);
        }
        public async Task Put<T>(T thing, Action<IQueryClause> clauser, Action<IJoiner> joiner) where T : class
        {
            //This portion calls the func via the transaction that uses
            //factory.Build to create the cypher and run it against the driver
            //This is done on the primary object thing for now
            var transaction = new CypherTransaction(_driver, _queryConfig, () =>
            {
                var factory = Neo4jFactory.BootstrapWithMerge(thing, clauser);
                factory.RemoveStaleConnections(thing);
                factory.OnCreateSet(thing);
                factory.OnMatchSet(thing, true);

                return factory;
            });

            if (transaction?.Factory != null)
            {
                joiner(transaction.Factory);
                PutChildren(thing, transaction, transaction.Factory);
                await transaction.Execute();
            }
        }

        public Task Patch<T>(T thing, bool removeStaleConnections = false) where T : class => Patch(thing, ObjectHelper.GetIdProp(thing.GetType()), j => { }, removeStaleConnections);
        public Task Patch<T>(T thing, Action<IJoiner> joiner, bool removeStaleConnections = false) where T : class => Patch(thing, ObjectHelper.GetIdProp(thing.GetType()), joiner, removeStaleConnections);
        public Task Patch<T>(T thing, Action<IQueryClause> clauser, bool removeStaleConnections = false) where T : class => Patch(thing, clauser, j => { }, removeStaleConnections);
        public async Task Patch<T>(T thing, PropertyInfo idProp, Action<IJoiner> joiner, bool removeStaleConnections = false) where T : class
        {
            var val = idProp.GetValue(thing) ?? throw new AmbiguousMatchException("An object was attempting to be saved without an identifier.  Why come it's unscannable?");
            await Patch(thing, q => q.Where<T>(idProp.Name, Is.Equal(val)), joiner, removeStaleConnections);
        }
        public async Task Patch<T>(T thing, Action<IQueryClause> clauser, Action<IJoiner> joiner, bool removeStaleConnections = false) where T : class
        {
            //This portion calls the func via the transaction that uses
            //factory.Build to create the cypher and run it against the driver
            //This is done on the primary object thing for now
            var transaction = new CypherTransaction(_driver, _queryConfig, () =>
            {
                var factory = Neo4jFactory.BootstrapWithMerge(thing, clauser);
                if (removeStaleConnections)
                    factory.RemoveStaleConnections(thing);
                factory.OnMatchSet(thing, false);

                return factory;
            });

            if (transaction?.Factory != null)
            {
                joiner(transaction.Factory);
                PatchChildren(thing, transaction, transaction.Factory, removeStaleConnections);
                await transaction.Execute();
            }


        }

        private void PostChildren<T>(T thing, CypherTransaction transaction, IQueryFactory parentFactory)
        {
            var childFactory = parentFactory;
            WriteChildrenRecursively(thing, transaction, parentFactory, (child, prop, currentDepth, edge) =>
            {
                if (child == null)
                    return;

                transaction.ContinueWith<T>(factory =>
                {
                    childFactory = PostChild(thing, child, prop, parentFactory, childFactory, edge);
                });
                PostChildren(child, transaction, childFactory);
            },
            (dicInfo, key, child, prop, currentDepth, edge) =>
            {
                if (child == null)
                    return;

                transaction.ContinueWith<T>(factory =>
                {
                    childFactory = PostChild(thing, child, prop, parentFactory, childFactory, edge);
                });
                PostChildren(child, transaction, childFactory);
            });
        }
        private IQueryFactory PostChild(object? parent, object child, PropertyInfo prop, IQueryFactory parentFactory, IQueryFactory childFactory, GraphEdgeDetails edge)
        {

            if (parent == null)
                return childFactory;
            var idProp = ObjectHelper.GetIdProp(child.GetType());
            var idValue = idProp.GetValue(child);
            var edgeDetails = new Func<GraphEdgeDetails, object, PropertyInfo, GraphEdgeDetails>((edge, o, prop) => edge != null
                ? new GraphEdgeDetails { Label = edge.Label, Properties = edge.Properties, Direction = edge.Direction }
                : ObjectHelper.GetPropertyEdge(prop, label: prop?.Name ?? o.GetType().Name)
            );

            var matchable = idValue != null;
            if (matchable)
            {
                parentFactory.MergeChild(parent, child,
                    t => t.Where(idProp.Name, Is.Equal(idProp.GetValue(child)), child.GetType()),
                    subFactory =>
                    {
                        subFactory.OnCreateSet(child);
                        subFactory.OnMatchSet(child, false);
                    },
                    subFactory =>
                    {
                        childFactory = ConnectChildFactory(prop, parentFactory, subFactory, edge);
                        return edgeDetails(edge, child, prop);
                    }
                );
            }
            else
            {
                parentFactory.CreateChild(parent, child, prop,
                    subFactory => childFactory = (Neo4jFactory)subFactory,
                    subFactory =>
                    {
                        childFactory = ConnectChildFactory(prop, parentFactory, subFactory, edge);
                        return edgeDetails(edge, child, prop);
                    }
                );
            }

            return childFactory;
        }
        private void PutChildren<T>(T thing, CypherTransaction transaction, IQueryFactory parentFactory)
        {
            var childFactory = parentFactory;
            WriteChildrenRecursively(thing, transaction, parentFactory, (child, prop, currentDepth, edge) =>
            {
                if (child == null)
                    return;

                transaction.ContinueWith<T>(factory =>
                {
                    childFactory = PutChild(thing, child, prop, parentFactory, childFactory, edge);
                });
                PutChildren(child, transaction, childFactory);
            },
            (dicInfo, key, child, prop, currentDepth, edge) =>
            {
                if (child == null)
                    return;

                transaction.ContinueWith<T>(factory =>
                {
                    childFactory = PutChild(thing, child, prop, parentFactory, childFactory, edge);
                });
                PutChildren(child, transaction, childFactory);
            });
        }
        private IQueryFactory PutChild(object? parent, object child, PropertyInfo prop, IQueryFactory parentFactory, IQueryFactory childFactory, GraphEdgeDetails edge)
        {

            if (parent == null)
                return childFactory;
            var idProp = ObjectHelper.GetIdProp(child.GetType());
            var idValue = idProp.GetValue(child);
            var edgeDetails = new Func<GraphEdgeDetails, object, PropertyInfo, GraphEdgeDetails>((edge, o, prop) => edge != null
                ? new GraphEdgeDetails { Label = edge.Label, Properties = edge.Properties, Direction = edge.Direction }
                : ObjectHelper.GetPropertyEdge(prop, label: prop?.Name ?? o.GetType().Name)
            );

            var matchable = idValue != null;
            if (matchable)
            {
                parentFactory.MergeChild(parent, child,
                    t => t.Where(idProp.Name, Is.Equal(idProp.GetValue(child)), child.GetType()),
                    subFactory =>
                    {
                        subFactory.RemoveStaleConnections(child);
                        subFactory.OnCreateSet(child);
                        subFactory.OnMatchSet(child, false);
                    },
                    subFactory =>
                    {
                        childFactory = ConnectChildFactory(prop, parentFactory, subFactory, edge);
                        return edgeDetails(edge, child, prop);
                    }
                );
            }
            else
            {
                parentFactory.CreateChild(parent, child, prop,
                    subFactory => childFactory = (Neo4jFactory)subFactory,
                    subFactory =>
                    {
                        childFactory = ConnectChildFactory(prop, parentFactory, subFactory, edge);
                        return edgeDetails(edge, child, prop);
                    }
                );
            }

            return childFactory;
        }
        private void PatchChildren<T>(T thing, CypherTransaction transaction, IQueryFactory parentFactory, bool removeStaleConnections = false) where T : notnull
        {
            var childFactory = parentFactory;
            WriteChildrenRecursively(thing, transaction, parentFactory, (child, prop, currentDepth, edge) =>
            {
                if (child == null)
                    return;

                var alreadySeen = parentFactory.GlobalObjectRegistry.ContainsKey(child.GetHashCode());
                if (alreadySeen)
                {
                    var parentNeo = (Neo4jFactory)parentFactory;
                    if (parentNeo.GlobalObjectRegistry.TryGetValue(child.GetHashCode(), out var existingChildFactory))
                    {
                        var edgeDetails = edge != null
                            ? new GraphEdgeDetails { Label = edge.Label, Properties = edge.Properties, Direction = edge.Direction }
                            : ObjectHelper.GetPropertyEdge(prop, label: prop?.Name ?? child.GetType().Name);

                        parentNeo.ConnectEdges(parentNeo, existingChildFactory, _ => edgeDetails);
                    }
                    return;
                }

                transaction.ContinueWith<T>(factory =>
                {
                    childFactory = PatchChild(thing, child, prop, parentFactory, childFactory, edge, removeStaleConnections);
                });
                PatchChildren(child, transaction, childFactory, removeStaleConnections);
            },
            (dicInfo, key, child, prop, currentDepth, edge) =>
            {
                if (child == null)
                    return;

                var alreadySeen = parentFactory.GlobalObjectRegistry.ContainsKey(child.GetHashCode());
                if (alreadySeen)
                {
                    var parentNeo = (Neo4jFactory)parentFactory;
                    if (parentNeo.GlobalObjectRegistry.TryGetValue(child.GetHashCode(), out var existingChildFactory))
                    {
                        var edgeDetails = edge != null
                            ? new GraphEdgeDetails { Label = edge.Label, Properties = edge.Properties, Direction = edge.Direction }
                            : ObjectHelper.GetPropertyEdge(prop, label: prop?.Name ?? child.GetType().Name);

                        parentNeo.ConnectEdges(parentNeo, existingChildFactory, _ => edgeDetails);
                    }
                    return;
                }

                transaction.ContinueWith<T>(factory =>
                {
                    childFactory = PatchChild(thing, child, prop, parentFactory, childFactory, edge, removeStaleConnections);
                });
                PatchChildren(child, transaction, childFactory, removeStaleConnections);
            });
        }
        private IQueryFactory PatchChild(object? parent, object child, PropertyInfo prop, IQueryFactory parentFactory, IQueryFactory childFactory, GraphEdgeDetails edge, bool removeStaleConnections = false)
        {

            if (parent == null)
                return childFactory;
            var idProp = ObjectHelper.GetIdProp(child.GetType());
            var idValue = idProp.GetValue(child);
            var edgeDetails = new Func<GraphEdgeDetails, object, PropertyInfo, GraphEdgeDetails>((edge, o, prop) => edge != null
                ? new GraphEdgeDetails { Label = edge.Label, Properties = edge.Properties, Direction = edge.Direction }
                : ObjectHelper.GetPropertyEdge(prop, label: prop?.Name ?? o.GetType().Name)
            );

            var matchable = idValue != null;
            if (matchable)
            {
                parentFactory.MergeChild(parent, child,
                    t => t.Where(idProp.Name, Is.Equal(idProp.GetValue(child)), child.GetType()),
                    subFactory =>
                    {
                        if (removeStaleConnections)
                            subFactory.RemoveStaleConnections(child);

                        subFactory.OnCreateSet(child);
                        subFactory.OnMatchSet(child, false);
                    },
                    subFactory =>
                    {
                        childFactory = ConnectChildFactory(prop, parentFactory, subFactory, edge);
                        return edgeDetails(edge, child, prop);
                    }
                );
            }
            else
            {
                parentFactory.CreateChild(parent, child, prop,
                    subFactory => childFactory = (Neo4jFactory)subFactory,
                    subFactory =>
                    {
                        childFactory = ConnectChildFactory(prop, parentFactory, subFactory, edge);
                        return edgeDetails(edge, child, prop);
                    }
                );
            }

            return childFactory;
        }
        private IQueryFactory ConnectChildFactory(PropertyInfo prop, IQueryFactory parentFactory, ISubQueryFactory subFactory, GraphEdgeDetails edge)
        {
            var childFactory = (Neo4jFactory)subFactory;
            var primitivity = ObjectHelper.GetPrimitivity(prop.PropertyType);
            if (primitivity.HasFlag(GraphPrimitivity.Dictionary))
            {
                var dicInfo = ObjectHelper.GetDictionaryInfo(prop.PropertyType);
                if (dicInfo.KeyPrimitivity.HasFlag(GraphPrimitivity.Object))
                    throw new NotImplementedException("Non primitive dictionary keys are not yet supported");

                var keyed = prop.GetCustomAttribute<GraphKeyLabellingAttribute>() != null;
                var key = keyed
                    ? $"{prop.Name}.{edge.Label}"
                    : prop.Name;
                if (!parentFactory.RootVar.Connections.TryGetValue(key, out var connection))
                    throw new DataMisalignedException("Unable to determine filter for child objects");

                childFactory.RootVar.SaveChildFilter = connection.SaveChildFilter != null ? [.. connection.SaveChildFilter] : null;
                childFactory.RootVar.Connections = connection.Connections;

            }
            else
            {
                if (!parentFactory.RootVar.Connections.TryGetValue(prop.Name, out var connection))
                    throw new DataMisalignedException("Cannot find connection to parent factory");
                childFactory.RootVar.SaveChildFilter = connection.SaveChildFilter != null ? [.. connection.SaveChildFilter] : null;
                childFactory.RootVar.Connections = connection.Connections;
            }
            return childFactory;
        }
        private void WriteChildrenRecursively<T>(
            T thing,
            CypherTransaction transaction,
            IQueryFactory parentFactory,
            Action<object?, PropertyInfo, CypherVar, GraphEdgeDetails> objectAction,
            Action<DictionaryInfo, object?, object?, PropertyInfo, CypherVar, GraphEdgeDetails> dicAction)
        {
            if (thing == null)
                return;

            var children = thing.GetType().GetProperties().Select(prop => prop).Where(prop =>
                (parentFactory.RootVar.SaveChildFilter?.Contains(prop.Name) ?? false)
                && prop.GetAttribute<GraphIgnoreAttribute>() == null
                && prop.GetValue(thing) != null
                && (ObjectHelper.GetPrimitivity(prop.PropertyType).HasFlag(GraphPrimitivity.Object)
                    || ObjectHelper.GetPrimitivity(prop.PropertyType).HasFlag(GraphPrimitivity.Dictionary))
                && !ObjectHelper.IsSerializable(prop));

            foreach (var prop in children)
            {
                var subThing = prop.GetValue(thing) ?? new DataMisalignedException($"Unable to read property {thing.GetType()}.{prop.Name}");
                var primitivity = ObjectHelper.GetPrimitivity(subThing.GetType());

                if (!primitivity.HasFlag(GraphPrimitivity.Array))
                {
                    if (!parentFactory.RootVar.Connections.TryGetValue(prop.Name, out var childVar))
                        throw new DataMisalignedException("Unable to determine filter for child objects");
                    objectAction(subThing, prop, childVar, ObjectHelper.GetPropertyEdge(prop));
                }
                else
                {
                    Action<object, object?, GraphEdgeDetails?, CypherVar?>? onEach = null;
                    onEach = new Action<object, object?, GraphEdgeDetails?, CypherVar?>((parent, child, preferredEdge, parentVar) =>
                    {
                        var parentPrimitivity = ObjectHelper.GetPrimitivity(parent);
                        var childPrimitivity = ObjectHelper.GetPrimitivity(child);

                        if (parentPrimitivity.HasFlag(GraphPrimitivity.Dictionary))
                        {
                            //if parent is dictionary, then this child is a key value pair
                            var dicInfo = ObjectHelper.GetDictionaryInfo(parent.GetType());
                            if (dicInfo.KeyPrimitivity.HasFlag(GraphPrimitivity.Object))
                                throw new NotImplementedException("Non primitive dictionary keys are not yet supported");

                            var keyValue = child?.GetType()?.GetProperty("Key")?.GetValue(child);
                            var valueValue = child?.GetType()?.GetProperty("Value")?.GetValue(child);
                            var edge = ObjectHelper.GetDictionaryEdge(dicInfo, prop, keyValue);
                            var key = edge.KeyLabelling != null
                                ? $"{prop.Name}.{edge.Label}"
                                : prop.Name;
                            if (!parentFactory.RootVar.Connections.TryGetValue(key, out var childVar))
                                throw new DataMisalignedException("Unable to determine filter for child objects");
                            if (dicInfo.ValPrimitivity.HasFlag(GraphPrimitivity.Array) || dicInfo.ValPrimitivity.HasFlag(GraphPrimitivity.Dictionary))
                            {
                                //DeepIteration(valueValue, onEach ?? ((_, _) => { }), edge);
                                DeepIteration(valueValue, onEach ?? ((_, _, _, _) => { }), childVar, edge);
                            }
                            else
                            {
                                dicAction(dicInfo, keyValue, valueValue, prop, childVar, edge);
                            }
                        }
                        else if (childPrimitivity.HasFlag(GraphPrimitivity.Array) || childPrimitivity.HasFlag(GraphPrimitivity.Dictionary))
                        {
                            DeepIteration(child, onEach ?? ((_, _, _, _) => { }), parentVar);
                        }
                        else
                        {
                            //if (!(parentVar?.Connections.TryGetValue($"{prop.Name}", out var childVar) ?? false))
                            //    throw new DataMisalignedException("Unable to determine filter for child objects");
                            objectAction(child, prop, parentVar ?? throw new DataMisalignedException("Unable to determine filter for child objects"), preferredEdge ?? ObjectHelper.GetPropertyEdge(prop));
                        }
                    });
                    DeepIteration(subThing, onEach, parentFactory.RootVar);
                }
            }

        }
        private void DeepIteration(object? enumerable, Action<object, object?, GraphEdgeDetails?, CypherVar?> each, CypherVar? parentVar, GraphEdgeDetails? preferredEdge = null)
        {
            object? enumerator;
            if (enumerable == null)
                return;

            var method = enumerable?.GetType()?.GetMethod("GetEnumerator");
            if (method != null && (enumerator = method.Invoke(enumerable, null)) != null)
            {
                // Get the 'MoveNext' and 'Current' properties for iteration
                var moveNextMethod = enumerator.GetType().GetMethod("MoveNext");
                var currentProperty = enumerator.GetType().GetProperty("Current");

                // Iterate through the dictionary using reflection
                if (currentProperty != null)
                {
                    while (((bool?)moveNextMethod?.Invoke(enumerator, null)) ?? false)
                    {
                        each(enumerable ?? new { }, currentProperty.GetValue(enumerator), preferredEdge, parentVar);
                    }
                }
            }
            else
            {
                throw new NotImplementedException($"This type of array {enumerable?.GetType()?.Name ?? "\"unknown\""} is not supported as a collection yet.");
            }
        }

        /// <summary>
        /// Execute a raw Cypher query with optional parameters
        /// </summary>
        /// <param name="cypher">The Cypher query to execute</param>
        /// <param name="parameters">Optional parameters for the query</param>
        /// <returns>The query result as a list of dictionaries where each dictionary represents a record</returns>
        public async Task<IList<IDictionary<string, object?>>> Execute(string cypher, Dictionary<string, object?>? parameters = null)
        {

            var results = new List<IDictionary<string, object?>>();
            var (queryResults, summary) = await new CypherTransaction(_driver, _queryConfig).Execute(cypher, parameters);
            var records = queryResults.ToList();

            foreach (var record in records)
            {
                var dict = new Dictionary<string, object?>();
                foreach (var key in record.Keys)
                {
                    dict[key] = record[key];
                }
                results.Add(dict);
            }

            return results;

        }
    }
}
