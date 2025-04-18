using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Domain;
using Chrono.Graph.Core.Notations;
using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace Chrono.Graph.Core.Utilities
{
    public static class ObjectHelper
    {
        public static string GetEdgeLabel(MemberInfo t) => GetLabel<GraphEdgeAttribute>(t, a => !string.IsNullOrEmpty(a?.Definition?.Label ?? "") ? a?.Definition?.Label ?? t.Name : t.Name);
        public static string GetEdgeLabel(PropertyInfo t)
        {
            var edgePropLabel = GetLabel<GraphEdgeAttribute>(t, a => !string.IsNullOrEmpty(a?.Definition?.Label ?? "") ? a?.Definition?.Label ?? "" : "");
            return !string.IsNullOrEmpty(edgePropLabel) 
                ? edgePropLabel 
                : GetLabel<GraphEdgeAttribute>(t.PropertyType, a => !string.IsNullOrEmpty(a?.Definition?.Label ?? t.Name) ? a?.Definition?.Label ?? t.Name : t.Name);
        }
        public static string GetPropertyLabel(Type t, string propName)
        {
            var propInfo = t.GetProperty(propName)
                ?? throw new DataMisalignedException($"Cannot find property name {propName}!");
            return GetPropertyLabel(propInfo);

        }
        public static string GetPropertyLabel(PropertyInfo t)
        {
            var primitivity = GetPrimitivity(t.PropertyType);
            return GetLabel<GraphLabelAttribute>(t, a => !string.IsNullOrEmpty(a?.Label ?? "") ? a?.Label ?? t.Name : t.Name);
        }

        //public static string GetObjectLabel(PropertyInfo t) => GetObjectLabel(t.PropertyType);
        public static string GetObjectLabel(Type t)
        {
            var primitivity = GetPrimitivity(t);
            if (primitivity.HasFlag(GraphPrimitivity.Dictionary))
            {
                var dic = GetDictionaryInfo(t);
                return GetObjectLabel(dic.ValType);
            }
            else if (primitivity.HasFlag(GraphPrimitivity.Array))
            {

                return GetObjectLabel(t.GenericTypeArguments[0]);
            }
            else
            {
                return GetLabel<GraphLabelAttribute>(t, a => !string.IsNullOrEmpty(a?.Label ?? "") ? a?.Label ?? t.Name : t.Name);
            }
        }

        public static string GetLabel<T>(MemberInfo t, Func<T?, string> valueFactory) where T : Attribute
        {
            var attrs = t.GetCustomAttributes(typeof(T), false);
            var any = attrs?.Any() ?? false;
            if (!any)
                return valueFactory(null);

            return valueFactory((T?)attrs?.FirstOrDefault());
        }
        public static T Instantiate<T>() => (T)(Activator.CreateInstance(typeof(T)) ?? throw new DataMisalignedException($"Cannot create C# object instance for {typeof(T).Name}"));
        public static object Instantiate(Type type) => Activator.CreateInstance(type) ?? throw new DataMisalignedException($"Cannot create C# object instance for {type.Name}");
        public static IEnumerable<PropertyInfo>? GetLabelProperty(Type parentType, string childEdgeLabel)
        {
            return parentType.GetProperties().Where(property =>
            {
                var propertyType = property.PropertyType;
                var propertyPrimitiviy = GetPrimitivity(propertyType);
                var keyLabeling = propertyPrimitiviy.HasFlag(GraphPrimitivity.Dictionary)
                    && property.GetCustomAttribute<GraphKeyLabellingAttribute>() != null;

                if (keyLabeling)
                {
                    var dic = GetDictionaryInfo(propertyType);
                    return dic.KeyType.IsEnum
                        ? GetDictionaryLabels(dic, property).Any(keyLabel => Utils.StandardizeEdgeLabel(keyLabel) == Utils.StandardizeEdgeLabel(childEdgeLabel))
                        : throw new NotImplementedException($"Key type [{dic.KeyType.Name}] cannot be used for labelling.  Only enum keys are supported for [GraphKeyLabelling] Dictionaries");
                }
                else
                {
                    return Utils.StandardizeEdgeLabel(GetEdgeLabel(property)) == Utils.StandardizeNodeLabel(childEdgeLabel);
                }
            });
        }
        public static GraphEdgeBasic GetDictionaryEdge(DictionaryInfo dic, PropertyInfo prop, object? key)
        {
            var keyLabelling = prop.GetCustomAttribute<GraphKeyLabellingAttribute>();
            var keyString = key?.ToString() ?? "";
            var properties = key != null && !string.IsNullOrEmpty(keyString)
                ? new Dictionary<string, string> { { "key", keyString }, { "enum", ((int)key).ToString() } }
                : new Dictionary<string, string> { };

            var label = keyLabelling != null
                 ? dic.KeyType.IsEnum
                    ? GetLabel<GraphEdgeAttribute>(dic.KeyType.GetMember(keyString)[0], edgeAttribute => GenerateDictionaryPropertyLabel(edgeAttribute, prop, key))
                    : throw new NotImplementedException($"Key type [{dic.KeyType.Name}] cannot be used for labelling.  Only enum keys are supported for [GraphKeyLabelling] Dictionaries")
                :  GenerateDictionaryPropertyLabel(null, prop, null);


            return new GraphEdgeBasic
            {
                Label = label,
                Properties = properties,
            };
        }

        public static PropertyInfo GetIdProp<T>() => GetIdProp(typeof(T));
        public static PropertyInfo GetIdProp(Type t) => t.GetRuntimeProperties()
            .Where(a => a.GetCustomAttributes(typeof(GraphIdentifierAttribute), false).Any())
            .FirstOrDefault()
                ?? t.GetRuntimeProperties()
                    .Where(a => a.Name.ToLower() == "id")
                    .FirstOrDefault()
                        ?? throw new Exception("Unable to determine Id");

        public static IList<string> GetDictionaryLabels(DictionaryInfo dic, PropertyInfo prop)
            => dic.KeyType.IsEnum
                ? Enum.GetValues(dic.KeyType).Cast<object?>()
                    .Select(key => GetLabel<GraphEdgeAttribute>(dic.KeyType.GetMember(key?.ToString() ?? "")[0], a => GenerateDictionaryPropertyLabel(a, prop, key))).ToList()
                : new List<string> { GetObjectLabel(dic.KeyType) };
        private static string GenerateDictionaryPropertyLabel(GraphEdgeAttribute? edgeAttribute, PropertyInfo prop, object? key) 
            => !string.IsNullOrEmpty(edgeAttribute?.Definition?.Label)
                ? edgeAttribute.Definition.Label
                : (!string.IsNullOrEmpty(key?.ToString() ?? "")
                    ? $"{prop.Name}_{key?.ToString() ?? ""}"
                    : prop.Name);
        public static GraphEdgeDetails GetPropertyEdge(MemberInfo prop, bool optional = false, string? label = null)
        {
            var attr = prop.GetCustomAttribute<GraphEdgeAttribute>()?.Definition
                ?? prop.GetType()?.GetCustomAttribute<GraphEdgeAttribute>()?.Definition;

            var result = new GraphEdgeDetails
                {
                    Label = !string.IsNullOrEmpty(label) 
                        ? label 
                        : !string.IsNullOrEmpty(attr?.Label) 
                            ? attr.Label
                            : GetEdgeLabel(prop),
                    Direction = attr?.Direction ?? GraphEdgeDirection.Out,
                    Optional = attr?.Optional ?? optional
                };
            return result;
        }
        public static bool IsSerializable(PropertyInfo propertyInfo)
        {
            var primitivity = GetPrimitivity(propertyInfo.PropertyType);
            if (!primitivity.HasFlag(GraphPrimitivity.Object)
                || (propertyInfo.GetCustomAttributes<GraphObjectSerializeAttribute>()?.Any() ?? false))
                return true;

            var check = new Func<Type, bool>(t => t.GetCustomAttributes<GraphObjectSerializeAttribute>()?.Any() ?? false);

            if (primitivity.HasFlag(GraphPrimitivity.Array) && !primitivity.HasFlag(GraphPrimitivity.Dictionary))
            {
                if (propertyInfo.PropertyType.IsArray)
                    return check(propertyInfo.PropertyType);

                return propertyInfo.PropertyType.GenericTypeArguments.Select(a => check(a)).Aggregate((a, b) => a || b);
            }
            return check(propertyInfo.PropertyType);

        }
        public static GraphPrimitivity GetPrimitivity<T>(T thing)
        {
            if (thing == null)
                throw new AccessViolationException("Cannot determine graph primitivity on a null object");
            return GetPrimitivity(thing.GetType());
        }
        public static GraphPrimitivity GetPrimitivity(Type t)
        {

            var result = TypeGauntlet(t);

            if (t.IsArray)
            {
                result |= GraphPrimitivity.Array;
            }
            else if (typeof(IDictionary).IsAssignableFrom(t) || t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var dicInfo = GetDictionaryInfo(t);

                //check for array keys, gets funky trying to check for arrays here
                result = dicInfo.ValPrimitivity;
                //result |= GraphPrimitivity.Object;
                result |= GraphPrimitivity.Dictionary;
                result |= GraphPrimitivity.Array;
            }
            else if ((typeof(ICollection).IsAssignableFrom(t) || typeof(IEnumerable).IsAssignableFrom(t)) && !typeof(string).IsAssignableFrom(t))
            {
                result = TypeGauntlet(t.GenericTypeArguments);
                result |= GraphPrimitivity.Array;
            }

            return result > 0 ? result : GraphPrimitivity.Object;

        }
        public static DictionaryInfo GetDictionaryInfo(Type t)
        {
            var keyType = t.GenericTypeArguments[0];
            var valType = t.GenericTypeArguments[1];
            return new DictionaryInfo
            {
                KeyType = keyType,
                KeyPrimitivity = GetPrimitivity(keyType),
                ValType = valType,
                ValPrimitivity = GetPrimitivity(valType)
            };

        }
        private static GraphPrimitivity TypeGauntlet(IEnumerable<Type> types)
        {
            var generics = types.Select(TypeGauntlet);
            var result = generics.FirstOrDefault(g => g > GraphPrimitivity.Object);
            result = result == 0 ? GraphPrimitivity.Object : result;
            return result;
        }
        private static GraphPrimitivity TypeGauntlet(Type t)
        {
            var result = GraphPrimitivity.Object;
            var checkBox = new Dictionary<GraphPrimitivity, Func<Type, bool>>{
                { GraphPrimitivity.String, tx => {
                    return tx == typeof(string) || tx == typeof(string[])
                        || tx == typeof(Guid) || tx == typeof(Guid?) || tx == typeof(Guid[])
                        || tx == typeof(DateTime) || tx == typeof(DateTime?) || tx == typeof(DateTime[])
                        || tx == typeof(TimeSpan) || tx == typeof(TimeSpan?) || tx == typeof(TimeSpan[]);
                }},
                { GraphPrimitivity.Bool, tx => {
                    return tx == typeof(bool) || tx == typeof(bool?) || tx == typeof(bool[]);
                }},
                { GraphPrimitivity.Int, tx => {
                    return tx == typeof(int) || tx == typeof(int?) || tx == typeof(int[])
                     || IsEnumType(tx);
                }},
                { GraphPrimitivity.Float, tx =>
                {
                    return tx == typeof(float) || tx == typeof(float?) || tx == typeof(float[])
                        || tx == typeof(long) || tx == typeof(long?) || tx == typeof(long[])
                        || tx == typeof(double) || tx == typeof(double?) || tx == typeof(double[]);
                }}
            };

            result = checkBox.FirstOrDefault(c => c.Value(t)).Key; //default to non primitive
            return result == 0 ? GraphPrimitivity.Object : result;
        }
        private static bool IsEnumType(Type type)
            => Nullable.GetUnderlyingType(type) is Type underlyingType
                ? underlyingType.IsEnum
                : type.IsEnum;
        public static string Serialize<T>(T thing)
        {
            var primitivity = GetPrimitivity(thing);
            return primitivity.HasFlag(GraphPrimitivity.Object)
                ? JsonSerializer.Serialize(JsonSerializer.Serialize(thing), new JsonSerializerOptions { })
                : JsonSerializer.Serialize(thing);
        }

    }
}
