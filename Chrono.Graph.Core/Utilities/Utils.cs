using Castle.DynamicProxy;
using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Domain;
using NanoidDotNet;
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Chrono.Graph.Core.Utilities
{
    public static class Utils
    {
        public static object StandardizePropertyValue(object? value)
        {
            var primitivity = ObjectHelper.GetPrimitivity(value);
            if (value == null) return new object() { };
            if (value is Guid guidValue) return guidValue.ToString();
            if (value is DateTime dateTimeValue) return dateTimeValue.ToString("o"); // Convert DateTime to ISO 8601 format
            if (value.GetType().IsEnum) return (int)value;
            if (primitivity.HasFlag(GraphPrimitivity.Array))
            {
                var typeArgs = value.GetType().GetGenericArguments();
                if (typeArgs.Length > 0)
                {
                    var generic = typeArgs.First();
                    if (generic.IsEnum)
                    {
                        var enumArrayType = typeof(IEnumerable<>).MakeGenericType(generic);
                        var options = new JsonSerializerOptions();
                        options.Converters.Add(new JsonStringEnumConverter());

                        return JsonSerializer.Serialize(value, enumArrayType, options);
                    }
                }
            }
            if (primitivity.HasFlag(GraphPrimitivity.Object) || primitivity.HasFlag(GraphPrimitivity.Array) || primitivity.HasFlag(GraphPrimitivity.Dictionary)) return JsonSerializer.Serialize(value);

            return value; // Return value as is for types Neo4j already supports
        }

        public static string StandardizeGraphString(string value) => !Regex.IsMatch(CypherConstants.InvalidCharactersPattern, value)
            ? value.Length > 2048 ? value.Substring(0, 2048) : value
            : Regex.Replace(
                value.Length > 2048 ? value.Substring(0, 2048) : value,
                CypherConstants.InvalidCharactersPattern, " ");

        public static string ToTitleCase(this string value) =>
            new CultureInfo("en-US", false).TextInfo.ToTitleCase(value);
        public static string StandardizeVariableName(string value) => ToCamelCase(StandardizeGraphString(value));
        public static string StandardizePropertyName(string value) => StandardizeNodeLabel(value);
        public static string StandardizeNodeLabel (string value) 
            => !Regex.IsMatch(CypherConstants.InvalidCharactersPattern, value)
            ? value
            : new CultureInfo("en-US", false).TextInfo
                .ToTitleCase(
                    Regex.Replace(
                        value.Length > 2048 ? value[..2048] : value, 
                        CypherConstants.InvalidCharactersPattern, " ")
                    .ToLower())
                .Replace(" ", "");

        public static string StandardizeEdgeLabel (string value) 
            => Regex.Replace(
                value.Length > 2048 ? value[..2048] : value, 
                CypherConstants.InvalidCharactersPattern, "_")
            .ToUpper();
        public static string Id(string? existing = null) => string.IsNullOrEmpty(existing) || existing.Length != CypherConstants.SafeIdLength ? Nanoid.Generate(CypherConstants.SafeAlphabet, CypherConstants.SafeIdLength) : existing;
        public static string CypherId(string? existing = null) => Id(existing).Replace("-", "d").Replace("_", "u");
        public static void Recurse(this Dictionary<string, CypherVar> dic, Action<CypherVar> doWhatWithTheChildren)
        {
            foreach(var record in dic)
            {
                doWhatWithTheChildren(record.Value);
                if (record.Value.Connections != null && record.Value.Connections.Count > 0)
                    Recurse(record.Value.Connections, doWhatWithTheChildren);
            }
        }
        public static string GetExpressionPropertyName<T, TProperty>(this Expression<Func<T, TProperty>> expression) => GetExpressionProperty(expression).Name;
        public static PropertyInfo GetExpressionProperty<T, TProperty>(this Expression<Func<T, TProperty>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return (PropertyInfo) memberExpression.Member;
            }

            if (expression.Body is UnaryExpression unaryExpression)
            {
                return (PropertyInfo) ((MemberExpression)unaryExpression.Operand).Member;
            }

            throw new ArgumentException("Invalid expression");
        }
        public static void Merge<T, TT>(this Dictionary<T, TT> dic1, IReadOnlyDictionary<T, TT> dic2) where T : notnull
        {
            foreach(var injectable in dic2)
            {
                if(!dic1.TryAdd(injectable.Key, injectable.Value))
                {
                    var old = dic1[injectable.Key];
                    if (!(old?.Equals(injectable.Value) ?? false))
                        Console.WriteLine($"Warning: This cypher key {injectable.Key} has been added already with a different value");

                }

            }
        }
        public static string GetDescription<T>(this T thing) where T : struct
        {
            var type = thing.GetType();
            var name = thing.ToString();
            if (string.IsNullOrEmpty(name))
                return "";
            var memberInfo = type.GetMember(name);
            if (type.IsEnum && memberInfo.Length > 0)
            {
                var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs.Length > 0)
                    return ((DescriptionAttribute)attrs[0]).Description;
            }
            return name;
        }

        private static string? _version = null;
        public static string Version
        {
            get
            {
                _version ??= Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "vNA";
                return _version;
            }
        }
        public static string ToCamelCase(string phrase) => char.ToLowerInvariant(phrase[0]) + phrase[1..];

        /// <summary>
        /// WARNING: Every property must be marked virtual to be tracked this way.  
        /// This function creates a class that listens for changes from its original value and marks those fields as changed,
        /// thus allowing us to only act on what has changed and no more.  Most DB engines of course do a form of this as well during the
        /// write process making this work redudant if your using this to save less data to the DB.
        /// </summary>
        /// <typeparam name="T">The type of object we are tracking changes for</typeparam>
        /// <param name="thing">The hydrated object to be tracking changes for</param>
        /// <returns>The proxy object with the correct values</returns>
        public static T Snapshot<T>(this T thing) where T : class
        {
            var proxyGenerator = new ProxyGenerator();
            var changeTracker = new ChangeTrackingInterceptor();
            var proxy = proxyGenerator.CreateClassProxy<T>(changeTracker);

            foreach (var property in typeof(T).GetProperties())
            {
                if (property.CanWrite && property.CanRead && (property.GetMethod?.IsVirtual ?? false))
                {
                    var originalValue = property.GetValue(thing);
                    property.SetValue(proxy, originalValue);
                }
            }

            return proxy;
        }

    }

}
