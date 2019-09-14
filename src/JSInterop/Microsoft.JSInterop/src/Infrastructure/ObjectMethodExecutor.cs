using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.JSInterop.Infrastructure
{
    internal static class ObjectMethodExecutor
    {
        public static Func<object, object[], object> GetExecutor(MethodInfo methodInfo, Type targetType)
        {
            // Parameters to executor
            var targetParameter = Expression.Parameter(typeof(object), "target");
            var parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

            // Build parameter list
            var parameters = new List<Expression>();
            var paramInfos = methodInfo.GetParameters();
            for (var i = 0; i < paramInfos.Length; i++)
            {
                var paramInfo = paramInfos[i];
                var valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                var valueCast = Expression.Convert(valueObj, paramInfo.ParameterType);

                // valueCast is "(Ti) parameters[i]"
                parameters.Add(valueCast);
            }

            // Call method
            var instance = targetType is null ?
                null :
                Expression.Convert(targetParameter, targetType);
            var methodCall = Expression.Call(instance, methodInfo, parameters);

            // methodCall is "((Ttarget) target) method((T0) parameters[0], (T1) parameters[1], ...)"
            // Create function
            if (methodCall.Type == typeof(void))
            {
                var lambda = Expression.Lambda<Action<object, object[]>>(methodCall, targetParameter, parametersParameter);
                var voidExecutor = lambda.Compile();
                return WrapVoidAction(voidExecutor);
            }
            else
            {
                var castMethodCall = Expression.Convert(methodCall, typeof(object));
                var lambda = Expression.Lambda<Func<object, object[], object>>(castMethodCall, targetParameter, parametersParameter);
                return lambda.Compile();
            }

            static Func<object, object[], object> WrapVoidAction(Action<object, object[]> executor)
            {
                return delegate (object target, object[] parameters)
                {
                    executor(target, parameters);
                    return null;
                };
            }
        }
    }
}
