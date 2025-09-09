using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Chrono.Graph.Core.Utilities
{
  public static class JsonDefaults
  {
    private static JsonSerializerOptions? _options;
    public static JsonSerializerOptions Options => _options ??= Create();

    private static JsonSerializerOptions Create()
    {
      var options = new JsonSerializerOptions
      {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
      };
      options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
      var resolver = new DefaultJsonTypeInfoResolver();
      resolver.Modifiers.Add((typeInfo) =>
      {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
          return;
        foreach (var prop in typeInfo.Properties)
        {
          if (prop.PropertyType == typeof(string))
          {
            prop.ShouldSerialize = static (obj, value) => !string.IsNullOrEmpty((string?)value);
          }
        }
      });
      options.TypeInfoResolver = resolver;
      return options;
    }
  }
}






