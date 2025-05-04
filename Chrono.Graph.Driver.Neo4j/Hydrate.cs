using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Domain;
using Chrono.Graph.Core.Utilities;
using Neo4j.Driver;
using System.Collections;
using System.Reflection;
using System.Text.Json;


namespace Chrono.Graph.Adapter.Neo4j
{
    public static class Hydrate 
    {
        private static Dictionary<object, object> _cache = [];
        private static HashSet<string> _reads = [];
        /// <summary>
        /// Hydrates BOTH the object instance and the edges
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="record"></param>
        /// <param name="cypherVar"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void Instance(object instance, IRecord record, CypherVar cypherVar)
        {
            _cache = [];
            _reads = [];
            Edges(record, cypherVar);
            Recurse(instance,
                TryReadNode(record[cypherVar.Var], out var node) ? node : throw new ArgumentException("Node unreadable"),
                record,
                cypherVar
            );
        }
        private static object? GetFromCache(Type type, INode? node = null)
        {
            if (node == null)
                return null;

            var props = type.GetProperties();
            var idProp = ObjectHelper.GetIdProp(type);
            if (idProp == null)
                return null;

            var id = node.Properties.FirstOrDefault(p => p.Key.ToLower() == idProp.Name.ToLower()).Value;
            if (id == null)
                return null;

            if (_cache.TryGetValue(id, out var cached) && cached != null)
            {
                if (cached.GetType() != type)
                    throw new InvalidOperationException($"A cached version of this object [{type}] having id '{id}' is of a different type: {cached.GetType()}");

                return cached;
            }
            return null;

        }
        private static object Instantiate(Type type, INode? node = null)
        {
            var cached = GetFromCache(type, node);
            if (cached != null)
                return cached;

            var instance = ObjectHelper.Instantiate(type);
            if (node == null)
                return instance;

            var props = type.GetProperties();
            var idProp = ObjectHelper.GetIdProp(type);
            var id = node.Properties.FirstOrDefault(p => p.Key.ToLower() == idProp.Name.ToLower()).Value;
            if (id != null)
            {
                if(idProp.SetMethod != null)
                    idProp.SetValue(instance, id);
                _cache[id] = instance;
            }

            return instance;
        }

        private static void Edges(IRecord record, CypherVar cypherVar)
        {
            if(!string.IsNullOrEmpty(cypherVar.Edge?.Var) && record.TryGetValue(cypherVar.Edge.Var, out var edgeRecord))
            {
                IRelationship? edge = null;
                if((edgeRecord is List<object> edges))
                {
                    if((edges?.Count ?? 0) > 0)
                    {
                        var relationships = edgeRecord.As<List<IRelationship>>();
                        edge = relationships?.FirstOrDefault();
                    }
                }
                else
                {
                    edge = edgeRecord.As<IRelationship>();
                }
                if(edge != null)
                    cypherVar.Edge.Properties.Merge(edge.Properties?.ToDictionary(p => p.Key, p => p.Value?.ToString() ?? "") ?? [ ]);
            }

            foreach(var connected in cypherVar.Connections)
                Edges(record, connected.Value);

        }
        private static bool TryReadNode(object record, out INode? node)
        {
            node = record is List<object> records
                ? record.As<List<INode>>()?.FirstOrDefault()
                : record.As<INode>();
            return node != null;
        }
        private static bool TryReadNodes(object record, out IEnumerable<IGrouping<string, INode>> nodes)
        {
            nodes = record is List<object> records
                ? record.As<IEnumerable<INode>>().GroupBy(n => n.ElementId)
                : new List<INode> { record.As<INode>() }.GroupBy(n => n.ElementId);
            return nodes != null;

        }

        private static void Recurse(object? instance, INode? node, IRecord record, CypherVar cypherVar)
        {
            if (node == null || instance == null)
                return;

            Primitives(instance, node);

            foreach(var connectedVar in cypherVar.Connections.Where(c => record[c.Value.Var] != null))
            {

                var labelProperties = ObjectHelper.GetLabelProperty(instance.GetType(), connectedVar.Value.Edge?.Label ?? connectedVar.Value.Label);
                PropertyInfo? property;
                if((labelProperties?.Any() ?? false) && (property = labelProperties.FirstOrDefault()) != null )
                {
                    var primitivity = ObjectHelper.GetPrimitivity(property.PropertyType);
                    if (!primitivity.HasFlag(GraphPrimitivity.Array))
                    {
                        //BASIC OBJECTS
                        var connectedNode = TryReadNode(record[connectedVar.Value.Var], out var n) 
                            ? n 
                            : throw new ArgumentException("Node unreadable");

                        //Don't over-hydrate
                        //has this nodes objects property already been hydrated from this connected node?  dont do it again.
                        if (_reads.Add($"{node.ElementId}.{instance.GetType().Name}.{property.Name}.{connectedNode?.ElementId}"))
                        {
                            var connectedInstance = Instantiate(property.PropertyType, connectedNode);
                            Recurse(connectedInstance,
                                connectedNode,
                                record,
                                connectedVar.Value
                            );
                            property.SetValue(instance, connectedInstance);
                        }

                    }
                    else if (!primitivity.HasFlag(GraphPrimitivity.Dictionary))
                    {
                        //ARRAYS
                        var itemType = property.PropertyType.GenericTypeArguments[0];
                        var listType = typeof(List<>).MakeGenericType(itemType);
                        var instances = Instantiate(listType);
                        var buffer = new List<object?> { };
                        var addMethod = listType.GetMethod("Add") ?? throw new ArgumentException("Object is not of type dictionary");

                        var nodes = TryReadNodes(record[connectedVar.Value.Var], out var n) 
                            ? n 
                            : throw new ArgumentException("Node unreadable");
                        var parentPath = $"{node.ElementId}.{instance.GetType().Name}.{property.Name}";
                        Recurse(buffer, property.PropertyType.GenericTypeArguments[0], record, connectedVar.Value, node.ElementId, nodes, parentPath);
                        foreach (var thing in buffer)
                            addMethod.Invoke(instances, [thing]);

                        if (property.PropertyType.IsAssignableFrom(instances.GetType()))
                        {
                            //Prepend the existing items in the array that have been populated already
                            if (property.GetValue(instance) is IEnumerable<object> existingList)
                                foreach (var item in existingList)
                                    addMethod.Invoke(instances, [item]);

                            property.SetValue(instance, instances);
                        }
                        else
                        {
                            // Attempt a cast using reflection to ensure compatibility
                            var castMethod = typeof(Enumerable).GetMethod("Cast")?.MakeGenericMethod(itemType) ?? throw new FieldAccessException("The `Cast()` method is no longer supported by IEnumerable<>");
                            var toListMethod = typeof(Enumerable).GetMethod("ToList")?.MakeGenericMethod(itemType) ?? throw new FieldAccessException("The `ToList()` method is no longer supported by IEnumerable<>");
                            var castedEnumerable = castMethod.Invoke(null, new object[] { instances });
                            if (castedEnumerable != null)
                            {
                                var finalList = toListMethod.Invoke(null, new object[] { castedEnumerable });
                                property.SetValue(instance, finalList);
                            }
                        }

                    }
                    else
                    {
                        //DICTIONARIES
                        var keyType = property.PropertyType.GenericTypeArguments[0];
                        var valueType = property.PropertyType.GenericTypeArguments[1];
                        var dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                        var buffer = new Dictionary<object, object?> ();
                        if (_reads.Add($"{node.ElementId}.{instance.GetType().Name}.{property.Name}.{connectedVar.Value.Var}"))
                        {
                            var instances = Instantiate(dictType);
                            var addMethod = dictType.GetMethod("Add")
                                ?? throw new FieldAccessException("The `Add()` method is no longer supported by Dictionar<object, object>");

                            var parentPath = $"{node.ElementId}.{instance.GetType().Name}.{property.Name}";
                            Recurse(buffer, keyType, valueType, record, connectedVar.Value);
                            foreach (var thing in buffer)
                                addMethod.Invoke(instances, [thing.Key, thing.Value]);

                            var existingValues = property.GetValue(instance);
                            if (property.PropertyType.IsAssignableFrom(instances.GetType()))
                            {
                                if (existingValues != null
                                    && existingValues.GetType().IsGenericType
                                    && existingValues.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>))
                                {
                                    // Get existing dictionary keys and values
                                    var existingDict = existingValues as IDictionary;

                                    if (existingDict != null)
                                    {
                                        foreach (DictionaryEntry entry in (IDictionary)buffer)
                                        {
                                            if (!existingDict.Contains(entry.Key))
                                                existingDict.Add(entry.Key, entry.Value);
                                        }

                                        property.SetValue(instance, existingDict);
                                    }
                                }
                                else
                                {
                                    property.SetValue(instance, instances);
                                }
                            }
                            else
                            {
                                // Attempt a cast using reflection to ensure compatibility
                                var castMethod = typeof(Enumerable).GetMethod("Cast")?.MakeGenericMethod(typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType));
                                var toDictMethod = typeof(Enumerable).GetMethod("ToDictionary")?.MakeGenericMethod(keyType, valueType);
                                var castedEnumerable = castMethod?.Invoke(null, new object[] { instances });
                                if (castedEnumerable != null)
                                {
                                    var finalDict = toDictMethod?.Invoke(null, new object[] { castedEnumerable, (Func<object, object>)(entry => ((dynamic)entry).Key), (Func<object, object>)(entry => ((dynamic)entry).Value) });
                                    property.SetValue(instance, finalDict);
                                }

                            }
                        }
                    }
                }
            }
        }
        private static void Recurse(List<object?> instances, Type type, IRecord record, CypherVar cypherVar, string myNeo4JId, IEnumerable<IGrouping<string, INode>> nodes, string parentPath)
        {
            // 1. Collect valid Neo4j IDs from edge list, if defined
            HashSet<string>? allowedNodeIds = null;
            if (!string.IsNullOrEmpty(cypherVar.Edge?.Var) &&
                record.TryGetValue(cypherVar.Var, out var currentNodeObj) &&
                record.TryGetValue(cypherVar.Edge.Var, out var edgeRecordObj))
            {

                var edgeRecords = edgeRecordObj is List<object>
                    ? edgeRecordObj.As<List<IRelationship>>()
                    : new List<IRelationship> { edgeRecordObj.As<IRelationship>() };

                allowedNodeIds = edgeRecords
                    .Where(rel => rel.StartNodeElementId == myNeo4JId || rel.EndNodeElementId == myNeo4JId)
                    .Select(rel => rel.StartNodeElementId == myNeo4JId ? rel.EndNodeElementId : rel.StartNodeElementId)
                    .ToHashSet();
            }

            foreach (var nodeGroup in nodes)
            {
                var node = nodeGroup.First();
                var nodeId = node.ElementId;
                if (_reads.Add($"{parentPath}.{node.ElementId}"))
                {
                    // 2. If there are valid edge constraints, skip any node not connected by an edge
                    if (allowedNodeIds != null && !allowedNodeIds.Contains(nodeId))
                        continue;

                    var instance = Instantiate(type, node);
                    Recurse(instance, node, record, cypherVar);
                    instances.Add(instance);

                }

            }
        }

        private static void Recurse(Dictionary<object, object?> instances, Type keyType, Type valueType, IRecord record, CypherVar cypherVar)
        {
            if (!TryReadNodes(record[cypherVar.Var], out var nodes))
                throw new ArgumentException("Nodes unreadable");

            foreach (var node in nodes)
            {
                var keyInstance = Instantiate(keyType);
                //if an enum
                if (keyType.IsEnum && (cypherVar.Edge?.Properties?.TryGetValue("enum", out var enumValue) ?? false))
                {
                    keyInstance = Enum.Parse(keyType, enumValue.ToString());
                }
                else
                {
                    Recurse(keyInstance, node.First(), record, cypherVar);
                }
                var valuePrimitivity = ObjectHelper.GetPrimitivity(valueType);
                if (valuePrimitivity.HasFlag(GraphPrimitivity.Array))
                {
                    var valueGeneric = valueType.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(valueGeneric);
                    var valueInstance = Instantiate(valueGeneric, node.First());
                    var valueListInstance = Instantiate(listType);

                    Recurse(valueInstance, node.First(), record, cypherVar);

                    var valueAddMethod = listType.GetMethod("Add");
                    if(valueAddMethod != null)
                    {
                        valueAddMethod.Invoke(valueListInstance, [valueInstance]);

                        if (!instances.TryAdd(keyInstance, valueListInstance) && instances.TryGetValue(keyInstance, out var existingInstance))
                            valueAddMethod.Invoke(existingInstance, [valueInstance]);
                    }

                }
                else if (valuePrimitivity.HasFlag(GraphPrimitivity.Dictionary))
                {
                    var childDicKeyType = valueType.GenericTypeArguments[0];
                    var childDicValueType = valueType.GenericTypeArguments[1];
                    var childDictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                    var childBuffer = new Dictionary<object, object?>();
                    var childInstances = Instantiate(childDictType);
                    var addMethod = childDictType.GetMethod("Add");

                    Recurse(childBuffer, childDicKeyType, childDicValueType, record, cypherVar);

                    if(addMethod != null)
                        foreach (var thing in childBuffer)
                            addMethod.Invoke(childInstances, [thing.Key, thing.Value]);

                    if (!instances.TryAdd(keyInstance, childInstances))
                        instances[keyInstance] = childInstances;
                }
                else
                {
                    var valueInstance = Instantiate(valueType, node.First());
                    Recurse(valueInstance, node.First(), record, cypherVar);
                    if (!instances.TryAdd(keyInstance, valueInstance))
                        instances[keyInstance] = valueInstance;
                }

            }
        }

        private static void Primitives(object? instance, INode? node)
        {
            if (instance == null || node == null)
                return;

            var instanceProps = instance.GetType().GetProperties();

            foreach (var nodeProp in node.Properties)
            {
                var instanceProp = instanceProps.FirstOrDefault(op => ObjectHelper.GetPropertyLabel(op).ToLower() == nodeProp.Key.ToLower());
                if (instanceProp != null && instanceProp.SetMethod != null)
                {
                    if (instanceProp.PropertyType == typeof(string)
                        || instanceProp.PropertyType == typeof(bool)
                        || instanceProp.PropertyType == typeof(bool?))
                    {
                        instanceProp.SetValue(instance, nodeProp.Value.As(instanceProp.GetValue(instance)));
                    }
                    else if (instanceProp.PropertyType == typeof(int)) { instanceProp.SetValue(instance, nodeProp.Value.As<int>()); }
                    else if (instanceProp.PropertyType == typeof(int?)) { instanceProp.SetValue(instance, nodeProp.Value.As<int?>()); }
                    else if (instanceProp.PropertyType == typeof(DateTime)
                        || instanceProp.PropertyType == typeof(DateTime?))
                    {
                        instanceProp.SetValue(instance,
                            DateTime.TryParse(nodeProp.Value.As<string>(), out var date)
                                ? date
                                : throw new DataMisalignedException("Unable to read date value"));
                    }
                    else if (instanceProp.PropertyType == typeof(Guid))
                    {
                        instanceProp.SetValue(instance,
                            Guid.TryParse(nodeProp.Value.As<string>(), out var guid)
                                ? guid
                                : nodeProp.Value.As<string>() == "{}"
                                    ? Guid.Empty
                                    : throw new DataMisalignedException("Unable to read Guid"));
                    }
                    else if (instanceProp.PropertyType == typeof(Guid?))
                    {
                        instanceProp.SetValue(instance,
                            Guid.TryParse(nodeProp.Value.As<string>(), out var guid)
                                ? guid
                                : nodeProp.Value.As<string>() == "{}"
                                    ? Guid.Empty
                                    : null);
                    }
                    else if (instanceProp.PropertyType == typeof(IList<string>))
                    {

                        List<string> stringList = [];
                        var vals = nodeProp.Value.As<string>();
                        if(vals != "[]")
                        {
                            vals = vals.Trim();
                            vals = vals.StartsWith("\"") ? vals.Substring(1) : vals;
                            vals = vals.EndsWith("\"") ? vals.Substring(0, vals.Length - 1) : vals;
                            vals = vals.StartsWith("[") ? vals.Substring(1) : vals;
                            vals = vals.EndsWith("]") ? vals.Substring(0, vals.Length - 1) : vals;
                            stringList = vals.Split(',').Select(v =>
                            {
                                v = v.StartsWith("\"") ? v.Substring(1) : v;
                                v = v.EndsWith("\"") ? v.Substring(0, v.Length - 1) : v;
                                return v;
                            }).ToList();
                        }
                        instanceProp.SetValue(instance, stringList);
                    }
                    else if (instanceProp.PropertyType == typeof(IList<bool>)
                        || instanceProp.PropertyType == typeof(IList<bool?>))
                    {
                        instanceProp.SetValue(instance, nodeProp.Value.As<IList<bool>>());
                    }
                    else if (instanceProp.PropertyType.IsGenericType && 
                         instanceProp.PropertyType.GetGenericTypeDefinition() == typeof(HashSet<>))
                    {
                        var generic = instanceProp.PropertyType.GetGenericArguments()[0]; // Get the type argument (e.g., MyEnum)
                        var hashSetType = typeof(HashSet<>).MakeGenericType(generic); // Create the specific HashSet<MyEnum> type

                        // Deserialize the value as IEnumerable<string> for enums serialized as strings
                        IEnumerable<string>? items;
                        try
                        {
                            items = (JsonSerializer.Deserialize(nodeProp.Value?.ToString() ?? "", 
                                           typeof(IEnumerable<string>))) as IEnumerable<string>;
                        }
                        catch (JsonException)
                        {
                            items = new List<string>();
                        }

                        // Create an instance of the HashSet<MyEnum>
                        var hashSet = Instantiate(hashSetType);

                        // Add items to the HashSet using reflection
                        var addMethod = hashSetType.GetMethod("Add") 
                            ?? throw new FieldAccessException("The `Add()` method is no longer supported on HashSet<> objects");

                        foreach (var item in items ?? [])
                        {
                            // Convert string to enum
                            if (generic.IsEnum)
                            {
                                var convertedItem = Enum.Parse(generic, item);
                                addMethod.Invoke(hashSet, new[] { convertedItem });
                            }
                            else
                            {
                                addMethod.Invoke(hashSet, new[] { item });
                            }
                        }

                        // Set the populated HashSet to the property
                        instanceProp.SetValue(instance, hashSet);
                    }
                    else if (instanceProp.PropertyType == typeof(IList<int>)
                        || instanceProp.PropertyType == typeof(IList<int?>))
                    {
                        instanceProp.SetValue(instance, nodeProp.Value.As<IList<int>>());
                    }
                    else if (instanceProp.PropertyType == typeof(IList<DateTime>)
                        || instanceProp.PropertyType == typeof(IList<DateTime?>))
                    {

                        instanceProp.SetValue(instance, nodeProp.Value.As<IList<string>>()
                            .Select(s => DateTime.TryParse(s, out var dd) ? dd : DateTime.MinValue));
                    }
                    else
                    {
                        //lands here if the record has a value for this node property but the instance type is object
                        //if this were a connected object and not json there would be no node property in the record
                        var json = nodeProp.Value.As<string>();
                        var newObj = JsonSerializer.Deserialize(json, instanceProp.PropertyType);
                        instanceProp.SetValue(instance, newObj);
                    }
                }
            }
        }
    }
}
