using Chrono.Graph.Core.Application;
using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Domain;
using Chrono.Graph.Core.Utilities;
using Chrono.Graph.Notations;
using Neo4j.Driver;
using System.Reflection;


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
        public async Task<T?> GetScalar<T>(Action<IQueryClause> clauser, Action<IJoiner>? joiner) where T : class {
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
                    factory.Return(joiner);
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
                Hydrate.Instance(instance, record, factory.RootVar);
                return instance;
            }).Where(i => i != null).ToList() : [];
            return ((result?.Count ?? 0) > 0) ? result ?? [] : [];
        }


        /// <summary>
        /// Does NOT UPDATE child objects that already exist, 
        /// BUT Does ADD new child objects to child objects that already exist if they aren't added already
        /// Creates object and any child objects that do not exist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <returns></returns>
        public Task Post<T>(T thing) where T : class => Post(thing, 20);
        public async Task Post<T>(T thing, int depth) where T : class
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

            PostChildren(thing, transaction, depth, transaction.Factory);
            await transaction.Execute();
        }

        public Task Put<T>(T thing, Action<IQueryClause> clauser, Action<IJoiner> joiner, int depth) where T : class => throw new NotImplementedException();
        public Task Put<T>(T thing) where T : class => Put(thing, ObjectHelper.GetIdProp(thing.GetType()), 3);
        public Task Put<T>(T thing, int depth) where T : class => Put(thing, ObjectHelper.GetIdProp(thing.GetType()), depth);
        public Task Put<T>(T thing, PropertyInfo idProp, int depth) where T : class
        {
            var val = idProp.GetValue(thing);
            if (val == null)
                throw new AmbiguousMatchException("An object was attempting to be saved without an identifier.  Why come it's unscannable?");

            return Put(thing, q => q.Where<T>(idProp.Name, Is.Equal(val)), depth);
        }
        public async Task Put<T>(T thing, Action<IQueryClause> clauser, int depth) where T : class
        {
            //This portion calls the func via the transaction that uses
            //factory.Build to create the cypher and run it against the driver
            //This is done on the primary object thing for now
            var transaction = new CypherTransaction(_driver, _queryConfig, () =>
            {
                var factory = Neo4jFactory.BootstrapWithMerge(thing, clauser);
                factory.OnCreateSet(thing);
                factory.OnMatchSet(thing, true);

                return factory;
            });

            if (transaction?.Factory != null)
            {
                //if you Put instead of Post here you will overwrite already existing objects
                PostChildren(thing, transaction, depth, transaction.Factory);
                await transaction.Execute();
            }
        }


        public Task Patch<T>(T thing) where T : class => Patch(thing, 1);
        public Task Patch<T>(T thing, int depth) where T : class => Patch(thing, ObjectHelper.GetIdProp(thing.GetType()), depth);
        public Task Patch<T>(T thing, Action<IQueryClause> clauser) where T : class => Patch(thing, clauser, 1);
        public async Task Patch<T>(T thing, PropertyInfo idProp, int depth) where T : class
        {
            var val = idProp.GetValue(thing);
            if (val == null)
                throw new AmbiguousMatchException("An object was attempting to be saved without an identifier.  Why come it's unscannable?");

            await Patch(thing, q => q.Where<T>(idProp.Name, Is.Equal(val)), depth);
        }
        public async Task Patch<T>(T thing, Action<IQueryClause> clauser, int depth) where T : class
        {
            //This portion calls the func via the transaction that uses
            //factory.Build to create the cypher and run it against the driver
            //This is done on the primary object thing for now
            var transaction = new CypherTransaction(_driver, _queryConfig, () =>
            {
                var factory = Neo4jFactory.BootstrapWithMerge(thing, clauser);
                factory.OnMatchSet(thing, false);

                return factory;
            });

            if (transaction?.Factory != null)
            {
                PatchChildren(thing, transaction, depth, transaction.Factory);
                await transaction.Execute();
            }


        }


        public Task Delete<T>(Action<IQueryClause> clauser, Action<IJoiner> joiner) where T : class => throw new NotImplementedException();
        public Task Delete<T>(T thing, int nodeDepth) where T : class => throw new NotImplementedException();
        public async Task Delete<T>(T thing) where T : class => await Delete(thing, ObjectHelper.GetIdProp<T>());
        public async Task Delete<T>(T thing, PropertyInfo idProp) where T : class
        {
            var val = idProp.GetValue(thing);
            if (val == null)
                throw new AmbiguousMatchException("An object was attempting to be deleted without an identifier.  Why come it's unscannable?");

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


        public Task AddEdge<T, TT>(T from, string verb, TT to) where T : class
        {
            throw new NotImplementedException();
            //var fromIdProp = ObjectHelper.GetIdProp(from.GetType());
            //var toIdProp = ObjectHelper.GetIdProp(to.GetType());
            //await new CypherTransaction(_driver, _queryConfig).Execute<T>(() => {
            //    var factory = Neo4jFactory.BootstrapWithMatch<T>(c => c
            //        .Where(fromIdProp.Name, Is.Equal(fromIdProp.GetValue(from)), from.GetType()));
            //    factory.HotLink(verb, to);
            //    return factory;
            //});
        }



        private void PostChildren<T>(T thing, CypherTransaction transaction, int depth, IQueryFactory parentFactory)
        {
            var childFactory = parentFactory;
            RecurseChildren(thing, transaction, depth, parentFactory,
                (o, prop, currentDepth, edge) =>
                {
                    if (o == null) return;
                    transaction.ContinueWith<T>(factory => PostChild(thing, o, prop, parentFactory, childFactory, edge));
                    PostChildren(o, transaction, currentDepth, childFactory);
                },
                (dicInfo, key, o, prop, currentDepth, edge) => {
                    if (o == null) return;
                    transaction.ContinueWith<T>(factory => PostChild(thing, o, prop, parentFactory, childFactory, edge));
                    PostChildren(o, transaction, currentDepth, childFactory);
                }
            );
        }
        private void PostChild(object? thing, object o, PropertyInfo prop, IQueryFactory parentFactory, IQueryFactory childFactory, GraphEdgeBasic edge)
        {

            var idProp = ObjectHelper.GetIdProp(o.GetType());
            var idValue = idProp.GetValue(o);
            var edgeDetails = new Func<GraphEdgeBasic, object, PropertyInfo, GraphEdgeDetails>((edge, o, prop) => edge != null
                ? new GraphEdgeDetails { Label = edge.Label, Properties = edge.Properties }
                : ObjectHelper.GetPropertyEdge(prop, label: prop?.Name ?? o.GetType().Name)
            );

            var matchable = idValue != null;
            if (matchable)
            {
                parentFactory.MergeChild(thing, o,
                    t => t.Where(idProp.Name, Is.Equal(idProp.GetValue(o)), o.GetType()),
                    sub => sub.OnCreateSet(o),
                    () => edgeDetails(edge, o, prop)
                );
            }
            else
            {
                parentFactory.CreateChild(thing, o, prop,
                    f => childFactory = (Neo4jFactory)f,
                    () => edgeDetails(edge, o, prop)
                );
            }
        }
        private void PatchChildren<T>(T thing, CypherTransaction transaction, int depth, IQueryFactory parentFactory)
        {
            RecurseChildren(thing, transaction, depth, parentFactory,
                (o, prop, currentDepth, edge) =>
                {
                    if (o == null) return;

                    var type = o.GetType();
                    var idProp = ObjectHelper.GetIdProp(type);
                    var childFactory = parentFactory;
                    transaction.ContinueWith<T>(factory =>
                    {
                        parentFactory.MergeChild(thing, o,
                            clause => clause.Where(idProp.Name, Is.Equal(idProp.GetValue(o)), type),
                            subFactory =>
                            {
                                subFactory.OnMatchSet(o, false);
                                childFactory = (Neo4jFactory)subFactory;
                            },
                            () => new GraphEdgeDetails { Label = edge.Label, Properties = edge.Properties }
                        );
                    });
                },
                (dicInfo, key, o, prop, currentDepth, edge) =>
                {
                    if (o == null) return;
                    var childFactory = parentFactory;
                    transaction.ContinueWith<T>(factory =>
                    {
                        var type = o.GetType();
                        var idProp = ObjectHelper.GetIdProp(type);
                        //var edge = ObjectInfo.GetDictionaryEdge(dicInfo, prop, key);

                        parentFactory.MergeChild(thing, o,
                            clause => clause.Where(idProp.Name, Is.Equal(idProp.GetValue(o)), type),
                            subFactory =>
                            {
                                subFactory.OnMatchSet(o, false);
                                childFactory = (Neo4jFactory)subFactory;
                            },
                            () => new GraphEdgeDetails { Label = edge.Label, Properties = edge.Properties }
                        );
                    });

                    PatchChildren(o, transaction, currentDepth, childFactory);

                });
        }
        /// <summary>
        /// This should never be called.  Putting children means blindly overwriting existing data replacing properties with nulls
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <param name="transaction"></param>
        /// <param name="depth"></param>
        /// <param name="parentFactory"></param>
        [Obsolete("Always post or patch children.  Putting children will overwrite data")]
        private void PutChildren<T>(T thing, CypherTransaction transaction, int depth, IQueryFactory parentFactory)
        {
            RecurseChildren(thing, transaction, depth, parentFactory,
                (o, property, currentDepth, edge) =>
                {
                    if (o == null) return;

                    var type = o.GetType();
                    var idProp = ObjectHelper.GetIdProp(type);
                    var childFactory = parentFactory;
                    transaction.ContinueWith<T>(factory =>
                    {
                        //cant merge child because its already been added somewhere else?
                        //find where it was added and make the edge connection
                        parentFactory.MergeChild(thing, o,
                            clause => clause.Where(idProp.Name, Is.Equal(idProp.GetValue(o)), type),
                            subFactory =>
                            {
                                subFactory.OnCreateSet(o);
                                subFactory.OnMatchSet(o, true);
                                childFactory = (Neo4jFactory)subFactory;
                            }, () => {
                                var propEdge = ObjectHelper.GetPropertyEdge(property);
                                return new GraphEdgeDetails
                                {
                                    Label = edge?.Label ?? propEdge.Label,
                                    Properties = edge?.Properties ?? propEdge.Properties ?? []
                                };
                            });
                    });

                    PutChildren(o, transaction, currentDepth, childFactory);
                },
                (dicInfo, key, o, prop, currentDepth, edge) =>
                {

                    if (o == null) return;
                    var childFactory = parentFactory;
                    transaction.ContinueWith<T>(factory =>
                    {
                        var type = o.GetType();
                        var idProp = ObjectHelper.GetIdProp(type);

                        parentFactory.MergeChild(thing, o,
                        clause => clause.Where(idProp.Name, Is.Equal(idProp.GetValue(o)), type),
                        subFactory =>
                        {
                            subFactory.OnCreateSet(o);
                            subFactory.OnMatchSet(o, true);
                            childFactory = (Neo4jFactory)subFactory;
                        }, () => new GraphEdgeDetails { Label = edge.Label, Properties = edge.Properties });
                    });

                    PutChildren(o, transaction, currentDepth, childFactory);
                }
            );

        }
        private void RecurseChildren<T>(
            T thing,
            CypherTransaction transaction,
            int depth,
            IQueryFactory parentFactory,
            Action<object?, PropertyInfo, int, GraphEdgeBasic> objectAction,
            Action<DictionaryInfo, object?, object?, PropertyInfo, int, GraphEdgeBasic> dicAction)
        {
            if (depth == 0 || thing == null)
                return;

            depth--;

            foreach (var prop in thing.GetType().GetProperties()
                .Where(p => p.GetValue(thing) != null
                    && (ObjectHelper.GetPrimitivity(p.PropertyType).HasFlag(GraphPrimitivity.Object)
                        || ObjectHelper.GetPrimitivity(p.PropertyType).HasFlag(GraphPrimitivity.Dictionary))
                    && !ObjectHelper.IsSerializable(p)))
            {
                var subThing = prop.GetValue(thing) ?? new DataMisalignedException($"Unable to read property {thing.GetType()}.{prop.Name}");
                var primitivity = ObjectHelper.GetPrimitivity(subThing.GetType());

                if (!primitivity.HasFlag(GraphPrimitivity.Array))
                {
                    objectAction(subThing, prop, depth, ObjectHelper.GetPropertyEdge(prop));
                }
                else
                {
                    Action<object, object?, GraphEdgeBasic?>? onEach = null;
                    onEach = new Action<object, object?, GraphEdgeBasic?>((parent, child, preferredEdge) =>
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
                            if (dicInfo.ValPrimitivity.HasFlag(GraphPrimitivity.Array) || dicInfo.ValPrimitivity.HasFlag(GraphPrimitivity.Dictionary))
                            {
                                //DeepIteration(valueValue, onEach ?? ((_, _) => { }), edge);
                                DeepIteration(valueValue, onEach ?? ((_, _, _) => { }), edge);
                            }
                            else
                            {
                                dicAction(dicInfo, keyValue, valueValue, prop, depth, edge);
                            }
                        }
                        else if (childPrimitivity.HasFlag(GraphPrimitivity.Array) || childPrimitivity.HasFlag(GraphPrimitivity.Dictionary))
                        {
                            DeepIteration(child, onEach ?? ((_, _, _) => { }));
                        }
                        else
                        {
                            objectAction(child, prop, depth, preferredEdge ?? ObjectHelper.GetPropertyEdge(prop));
                        }
                    });
                    DeepIteration(subThing, onEach);
                }
            }
            depth++;

        }
        private void DeepIteration(object? enumerable, Action<object, object?, GraphEdgeBasic?> each, GraphEdgeBasic? preferredEdge = null)
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
                        each(enumerable ?? new { }, currentProperty.GetValue(enumerator), preferredEdge);
                    }
                }
            }
            else
            {
                throw new NotImplementedException($"This type of array {enumerable?.GetType()?.Name ?? "\"unknown\""} is not supported as a collection yet.");
            }
        }
    }
}
