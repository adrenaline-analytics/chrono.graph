using Castle.DynamicProxy;


namespace Chrono.Graph.Adapter.Neo4j
{
    public class ChangeTrackingInterceptor : IInterceptor
    {
        // Dictionary to store the changed properties
        private readonly Dictionary<string, object> _originalValues = new Dictionary<string, object>();

        public void Intercept(IInvocation invocation)
        {
            if (IsSetter(invocation.Method))
            {
                var propertyName = invocation.Method.Name.Substring(4); // Removes "set_"

                // Get the original value of the property (if it's tracked)
                if (!_originalValues.ContainsKey(propertyName))
                {
                    var currentValue = invocation.InvocationTarget.GetType()
                         .GetProperty(propertyName)?
                         .GetValue(invocation.InvocationTarget);

                    // Track original value
                    _originalValues[propertyName] = currentValue;
                }
            }

            invocation.Proceed();
        }

        private bool IsSetter(System.Reflection.MethodInfo method) => method.IsSpecialName && method.Name.StartsWith("set_");
        private bool IsGetter(System.Reflection.MethodInfo method) => method.IsSpecialName && method.Name.StartsWith("get_");
        public Dictionary<string, object> GetTrackedChanges() => _originalValues;
    }
}
