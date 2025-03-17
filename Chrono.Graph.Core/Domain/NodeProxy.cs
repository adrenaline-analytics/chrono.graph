using System.Reflection;

namespace Chrono.Graph.Core.Domain
{
    //Lazy loading and dirty data checker for objects returned from db
    public class NodeProxy<T> : DispatchProxy
    {
        private T _entity;
        private readonly Dictionary<string, bool> _dirtyProperties = new Dictionary<string, bool>();

        public NodeProxy(T entity) => _entity = entity;

        public bool IsPropertyDirty(string propertyName) => _dirtyProperties.ContainsKey(propertyName);
        public void SetEntity(T entity) => _entity = entity;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            // If it's a property setter, mark the property as dirty
            if (targetMethod?.Name?.StartsWith("set_") ?? false)
            {
                string propertyName = targetMethod.Name.Substring(4);
                _dirtyProperties[propertyName] = true;
            }

            return targetMethod?.Invoke(_entity, args);
        }
    }
}
