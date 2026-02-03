using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Domain;
using Chrono.Graph.Core.Utilities;
using Neo4j.Driver;


namespace Chrono.Graph.Adapter.Neo4j
{
    public class Hydrator2000
    {
        private Dictionary<(string Type, object Id), object> _cache = [];
        private HashSet<string> _reads = [];
        /// <summary>
        /// Hydrates BOTH the object instance and the edges
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="record"></param>
        /// <param name="cypherVar"></param>
        /// <exception cref="ArgumentException"></exception>
        public void Hydrate(object instance, IRecord record, CypherVar cypherVar)
        {
            _cache = [];
            _reads = [];

            var node = TryReadNode(record[cypherVar.Var], out var n) ? n : throw new ArgumentException("Node unreadable");
            var records = (record[cypherVar.Var] is IDictionary<string, object> s)
                ? s.ToDictionary(s => s.Key, s => s.Value)
                : record[cypherVar.Var] is INode recNode
                    ? new Dictionary<string, object> { { "node", recNode } }
                    : throw new ArgumentException("Record unreadable");

            Edges(record, cypherVar);
            Recurse(instance,
                node,
                records,
                cypherVar
            );
        }

        private object? GetFromCache(Type type, INode? node = null)
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

            if (_cache.TryGetValue((node.Labels.Aggregate((a, b) => $"{a}:{b}"), id), out var cached) && cached != null)
            {
                return cached;
            }
            return null;

        }
        private object Instantiate(Type type, INode? node = null)
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
                if (idProp.SetMethod != null)
                    idProp.SetValue(instance, id);
                _cache[(node.Labels.Aggregate((a, b) => $"{a}:{b}"), id)] = instance;
            }

            return instance;
        }

        private static void Edges(IRecord record, CypherVar cypherVar)
        {
            if (!string.IsNullOrEmpty(cypherVar.Edge?.Var) && record.TryGetValue(cypherVar.Var, out var wrappedList))
            {
                if (wrappedList is List<object> items)
                {
                    foreach (var item in items)
                    {
                        if (item is IDictionary<string, object> map && map.TryGetValue("edge", out var edgeObj) && edgeObj is IRelationship rel)
                        {
                            cypherVar.Edge.Properties.Merge(
                                rel.Properties?.ToDictionary(p => p.Key, p => p.Value?.ToString() ?? "")
                                ?? []);
                            break; // take the first valid edge only
                        }
                    }
                }
                else if (wrappedList is IDictionary<string, object> singleMap && singleMap.TryGetValue("edge", out var singleEdgeObj) && singleEdgeObj is IRelationship rel)
                {
                    cypherVar.Edge.Properties.Merge(
                        rel.Properties?.ToDictionary(p => p.Key, p => p.Value?.ToString() ?? "")
                        ?? []);
                }
            }

            foreach (var connected in cypherVar.Connections)
                Edges(record, connected.Value);
        }

        private static bool TryReadNode(object record, out INode? node)
        {
            node = null;

            if (record is IDictionary<string, object> map && map.TryGetValue("node", out var wrappedNodeObj))
            {
                node = wrappedNodeObj as INode;
                return node != null;
            }

            if (record is List<object> list)
            {
                foreach (var item in list)
                {
                    if (item is IDictionary<string, object> wrapped && wrapped.TryGetValue("node", out var wrappedNodeVal))
                    {
                        node = wrappedNodeVal as INode;
                        if (node != null)
                            return true;
                    }
                    else if (item is INode n)
                    {
                        node = n;
                        return true;
                    }
                }
            }

            if (record is INode directNode)
            {
                node = directNode;
                return true;
            }

            return false;
        }

        private static bool TryReadNodes(object record, out List<IGrouping<string, INode>> nodes)
        {
            nodes = new List<IGrouping<string, INode>>();

            if (record is List<Dictionary<string, object>> rawList)
            {
                var nodesOnly = new List<INode>();

                foreach (var item in rawList)
                {
                    if (item["node"] is INode wrappedNode)
                    {
                        nodesOnly.Add(wrappedNode);
                    }
                    else if (item is INode n)
                    {
                        nodesOnly.Add(n);
                    }
                }

                nodes = [.. nodesOnly.GroupBy(n => n.ElementId)];
                return nodes.Any();
            }

            if (record is INode singleNode)
            {
                nodes = [.. new List<INode> { singleNode }.GroupBy(n => n.ElementId)];
                return true;
            }

            return false;
        }


        private void Recurse(object? instance, INode? node, Dictionary<string, object> record, CypherVar cypherVar)
        {
            if (node == null || instance == null)
                return;

            Primitives(instance, node);

            foreach (var connectedVar in cypherVar.Connections)
            {

                var labelProperties = ObjectHelper.GetLabelProperty(instance.GetType(), connectedVar.Value.Edge?.Label ?? connectedVar.Value.Label);
                PropertyInfo? property;
                if (labelProperties?.Any() ?? false)
                {
                    // If multiple properties share the same edge label, disambiguate by checking the connected node's type.
                    // Try to read the connected node to get its labels and match against property element types.
                    INode? connectedNodeForTypeCheck = null;
                    if (labelProperties.Count() > 1 
                        && record.TryGetValue(connectedVar.Value.Var, out var rawForTypeCheck))
                    {
                        if (rawForTypeCheck is IList<object> rawListForCheck && rawListForCheck.Count > 0)
                        {
                            // For arrays, get the first node to determine the type
                            if (rawListForCheck[0] is IDictionary<string, object> firstItem 
                                && firstItem.TryGetValue("node", out var firstNodeObj))
                            {
                                connectedNodeForTypeCheck = firstNodeObj as INode;
                            }
                        }
                        else if (rawForTypeCheck is IDictionary<string, object> singleRecord 
                                 && singleRecord.TryGetValue("node", out var singleNodeObj))
                        {
                            connectedNodeForTypeCheck = singleNodeObj as INode;
                        }
                    }

                    // Match property by node labels if we have multiple candidates
                    if (connectedNodeForTypeCheck != null && labelProperties.Count() > 1)
                    {
                        property = labelProperties.FirstOrDefault(p =>
                        {
                            var primitivity = ObjectHelper.GetPrimitivity(p.PropertyType);
                            var elementType = primitivity.HasFlag(GraphPrimitivity.Array) && !primitivity.HasFlag(GraphPrimitivity.Dictionary)
                                ? p.PropertyType.GenericTypeArguments.FirstOrDefault() ?? p.PropertyType
                                : p.PropertyType;
                            var expectedLabel = ObjectHelper.GetObjectLabel(elementType);
                            //var secondaryLabels = ObjectHelper.GetObjectSecondaryLabels(elementType);
							// Check if any of the node's labels match the expected label or secondary labels
							return connectedNodeForTypeCheck.Labels.Any(nl =>
								Utils.StandardizeNodeLabel(nl) == Utils.StandardizeNodeLabel(expectedLabel));
                                //|| secondaryLabels.Any(sl => Utils.StandardizeNodeLabel(nl) == Utils.StandardizeNodeLabel(sl)));
                        }) ?? labelProperties.FirstOrDefault();
                    }
                    else
                    {
                        property = labelProperties.FirstOrDefault();
                    }
                }
                else
                {
                    property = null;
                }

                if (property != null)
                {
                    var primitivity = ObjectHelper.GetPrimitivity(property.PropertyType);
                    if (!primitivity.HasFlag(GraphPrimitivity.Array))
                    {
                        //BASIC OBJECTS
                        if (record.TryGetValue(connectedVar.Value.Var, out var rawConnected)
                            && rawConnected is IList<object> rawList
                            && ObjectHelper.TryMakeDictionary(rawList, out var connectedRecord)
                            && TryReadNode(connectedRecord, out var connectedNode))
                        {
                            //Don't over-hydrate
                            //has this nodes objects property already been hydrated from this connected node?  dont do it again.
                            if (_reads.Add($"{node.ElementId}.{instance.GetType().Name}.{property.Name}.{connectedNode?.ElementId}"))
                            {
                                var connectedInstance = Instantiate(property.PropertyType, connectedNode);
                                Recurse(connectedInstance,
                                    connectedNode,
                                    connectedRecord,
                                    connectedVar.Value
                                );
                                property.SetValue(instance, connectedInstance);
                            }
                        }
                    }
                    else if (!primitivity.HasFlag(GraphPrimitivity.Dictionary))
                    {
                        //ARRAYS
                        var itemType = property.PropertyType.GenericTypeArguments[0];
                        var listType = typeof(List<>).MakeGenericType(itemType);
                        var instances = Instantiate(listType);
                        var buffer = new List<object?> { };
                        var addMethod = listType.GetMethod("Add") ?? throw new ArgumentException("Object is not of type list");

                        if (record.TryGetValue(connectedVar.Value.Var, out var rawConnected)
                            && rawConnected is IList<object> rawList
                            && ObjectHelper.TryMakeDictionaries(rawList, out var connectedRecords)
                            && TryReadNodes(connectedRecords, out var connectedNodes))
                        {

                            var parentPath = $"{node.ElementId}.{instance.GetType().Name}.{property.Name}";
                            Recurse(buffer, property.PropertyType.GenericTypeArguments[0], connectedRecords, connectedVar.Value, node.ElementId, connectedNodes, parentPath);

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
                    }
                    else
                    {
                        //DICTIONARIES
                        var keyType = property.PropertyType.GenericTypeArguments[0];
                        var valueType = property.PropertyType.GenericTypeArguments[1];
                        var dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                        var buffer = new Dictionary<object, object?>();
                        if (_reads.Add($"{node.ElementId}.{instance.GetType().Name}.{property.Name}.{connectedVar.Value.Var}")
                            && record.TryGetValue(connectedVar.Value.Var, out var rawConnected)
                            && rawConnected is IList<object> rawList
                            && ObjectHelper.TryMakeDictionaries(rawList, out var connectedRecords))
                        {
                            var instances = Instantiate(dictType);
                            var addMethod = dictType.GetMethod("Add")
                                ?? throw new FieldAccessException("The `Add()` method is no longer supported by Dictionary<object, object>");

                            Recurse(buffer, keyType, valueType, connectedRecords, connectedVar.Value);
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
        private void Recurse(List<object?> instances, Type type, List<Dictionary<string, object>> records, CypherVar cypherVar, string myNeo4JId, IEnumerable<IGrouping<string, INode>> nodes, string parentPath)
        {
            // 1. Collect valid Neo4j IDs from edge list, if defined
            foreach (var record in records)
            {
                //HashSet<string>? allowedNodeIds = null;
                if (!string.IsNullOrEmpty(cypherVar.Edge?.Var) &&
                    record.TryGetValue("node", out var nodeObj) &&
                    record.TryGetValue("edge", out var edgeRecordObj) &&
                    TryReadNode(record, out var node) &&
                    node != null)
                {
                    var nodeId = node.ElementId;
                    if (_reads.Add($"{parentPath}.{node.ElementId}"))
                    {
                        var instance = Instantiate(type, node);
                        Recurse(instance, node, record, cypherVar);
                        instances.Add(instance);
                    }

                    //var edgeRecords = edgeRecordObj is List<object>
                    //    ? edgeRecordObj.As<List<IRelationship>>()
                    //    : new List<IRelationship> { edgeRecordObj.As<IRelationship>() };

                    //allowedNodeIds = edgeRecords
                    //    .Where(rel => rel.StartNodeElementId == myNeo4JId || rel.EndNodeElementId == myNeo4JId)
                    //    .Select(rel => rel.StartNodeElementId == myNeo4JId ? rel.EndNodeElementId : rel.StartNodeElementId)
                    //    .ToHashSet();
                    //}

                    //foreach (var nodeGroup in nodes)
                    //{
                    //var node = nodeGroup.First();
                    // 2. If there are valid edge constraints, skip any node not connected by an edge
                    //if (allowedNodeIds != null && !allowedNodeIds.Contains(nodeId))
                    //    continue;

                }
            }
        }

        private void Recurse(IDictionary<object, object?> instances, Type keyType, Type valueType, List<Dictionary<string, object>> records, CypherVar cypherVar)
        {
            if (!TryReadNodes(records, out var nodes))
                return;
            //throw new ArgumentException("Nodes unreadable");

            for (var i = 0; i < records.Count; i++)
            {
                var keyInstance = Instantiate(keyType);
                //if an enum
                if (keyType.IsEnum && records[i].TryGetValue("edge", out var recordEdge)
                    && recordEdge is IRelationship edge
                    && (edge.Properties?.TryGetValue("enum", out var enumValue) ?? false)
                    && enumValue != null)
                {
                    keyInstance = Enum.Parse(keyType, enumValue.ToString() ?? "");
                }
                else
                {
                    Recurse(keyInstance, nodes[i].First(), records[i], cypherVar);
                }
                var valuePrimitivity = ObjectHelper.GetPrimitivity(valueType);
                if (valuePrimitivity.HasFlag(GraphPrimitivity.Array))
                {
                    var valueGeneric = valueType.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(valueGeneric);
                    var valueInstance = Instantiate(valueGeneric, nodes[i].First());
                    var valueListInstance = Instantiate(listType);

                    Recurse(valueInstance, nodes[i].First(), records[i], cypherVar);

                    var valueAddMethod = listType.GetMethod("Add");
                    if (valueAddMethod != null)
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

                    Recurse(childBuffer, childDicKeyType, childDicValueType, records, cypherVar);

                    if (addMethod != null)
                        foreach (var thing in childBuffer)
                            addMethod.Invoke(childInstances, [thing.Key, thing.Value]);

                    if (!instances.TryAdd(keyInstance, childInstances))
                        instances[keyInstance] = childInstances;
                }
                else
                {
                    //TODO Bug, two different edges pointed to same node using a dictionary key edge
                    //records has 3 items, nodes only has 2
                    //nodes[2] out of range, nodes[1][1] is where value resides
                    //record 0 = node[0][0]
                    //record 1 = node[1][0]
                    //record 2 = node[1][1] -- we look for it at node[2][0] which doesnt exists
                    //.First() is the issue, need to recurse more intelligently
                    var valueInstance = Instantiate(valueType, nodes[i].First());
                    Recurse(valueInstance, nodes[i].First(), records[i], cypherVar);
                    if (!instances.TryAdd(keyInstance, valueInstance))
                        instances[keyInstance] = valueInstance;
                }

            }
        }

        private void Primitives(object? instance, INode? node)
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
                            DateTime.TryParse(nodeProp.Value.As<string>(), CultureInfo.InvariantCulture,
                                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                                out var date)
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
                        if (vals != "[]")
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
                            //TODO this logic needs to be ported to all enumeerable types
                            // Could be a Neo4j array, populate items from it as strings
                            if (nodeProp.Value is IEnumerable<object> rawArray)
                            {
                                items = rawArray.Select(x => x?.ToString() ?? "").ToList();
                            }
                            else
                            {
                                items = (JsonSerializer.Deserialize(nodeProp.Value?.ToString() ?? "",
                                               typeof(IEnumerable<string>))) as IEnumerable<string>;
                            }
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
                                var convertedItem = Enum.Parse(generic, item, true);
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
                        var newObj = JsonSerializer.Deserialize(json, instanceProp.PropertyType, Chrono.Graph.Core.Utilities.JsonDefaults.Options);
                        instanceProp.SetValue(instance, newObj);
                    }
                }
            }
        }
    }
}
