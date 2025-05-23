﻿using Castle.DynamicProxy;


namespace Chrono.Graph.Core.Domain
{
    public class ChangeTrackingInterceptor : IInterceptor
    {
        // Dictionary to store the changed properties
        private readonly Dictionary<string, object> _originalValues = [];

        public void Intercept(IInvocation invocation)
        {
            if (IsSetter(invocation.Method))
            {
                var propertyName = invocation.Method.Name[4..]; // Removes "set_"

                // Get the original value of the property (if it's tracked)
                if (!_originalValues.ContainsKey(propertyName))
                {
                    var currentValue = invocation.InvocationTarget.GetType()
                         .GetProperty(propertyName)?
                         .GetValue(invocation.InvocationTarget);

                    // Track original value
                    if(currentValue != null) 
                        _originalValues[propertyName] = currentValue;
                }
            }

            invocation.Proceed();
        }

        private static bool IsSetter(System.Reflection.MethodInfo method) => method.IsSpecialName && method.Name.StartsWith("set_");
        private static bool IsGetter(System.Reflection.MethodInfo method) => method.IsSpecialName && method.Name.StartsWith("get_");
        public Dictionary<string, object> GetTrackedChanges() => _originalValues;
    }
}
