// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Internal
{
    public class ObjectMethodExecutor
    {
        private readonly object[] _parameterDefaultValues;
        private readonly MethodExecutorAsync _executorAsync;
        private readonly MethodExecutor _executor;

        private static readonly ConstructorInfo _objectMethodExecutorAwaitableConstructor =
            typeof(ObjectMethodExecutorAwaitable).GetConstructor(new[] {
                typeof(object),                 // customAwaitable
                typeof(Func<object, object>),   // getAwaiterMethod
                typeof(Func<object, bool>),     // isCompletedMethod
                typeof(Func<object, object>),   // getResultMethod
                typeof(Action<object, Action>), // onCompletedMethod
                typeof(Action<object, Action>)  // unsafeOnCompletedMethod
            });

        private ObjectMethodExecutor(MethodInfo methodInfo, TypeInfo targetTypeInfo, object[] parameterDefaultValues)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            MethodInfo = methodInfo;
            MethodParameters = methodInfo.GetParameters();
            TargetTypeInfo = targetTypeInfo;
            MethodReturnType = methodInfo.ReturnType;

            var isAwaitable = IsAwaitableDirectlyOrViaCoercion(MethodReturnType,
                out var coerceToAwaitableExpression,
                out var coerceToAwaitableType,
                out var getAwaiterMethod,
                out var awaiterType,
                out var awaiterResultType,
                out var awaiterIsCompletedProperty,
                out var awaiterGetResultMethod,
                out var awaiterOnCompletedMethod,
                out var awaiterUnsafeOnCompletedMethod);

            IsMethodAsync = isAwaitable;
            AsyncResultType = isAwaitable ? awaiterResultType : null;

            // Upstream code may prefer to use the sync-executor even for async methods, because if it knows
            // that the result is a specific Task<T> where T is known, then it can directly cast to that type
            // and await it without the extra heap allocations involved in the _executorAsync code path.
            _executor = GetExecutor(methodInfo, targetTypeInfo);

            if (IsMethodAsync)
            {
                _executorAsync = GetExecutorAsync(
                    methodInfo,
                    targetTypeInfo,
                    coerceToAwaitableExpression,
                    coerceToAwaitableType,
                    getAwaiterMethod,
                    awaiterType,
                    awaiterResultType,
                    awaiterIsCompletedProperty,
                    awaiterGetResultMethod,
                    awaiterOnCompletedMethod,
                    awaiterUnsafeOnCompletedMethod);
            }

            _parameterDefaultValues = parameterDefaultValues;
        }

        private delegate ObjectMethodExecutorAwaitable MethodExecutorAsync(object target, object[] parameters);

        private delegate object MethodExecutor(object target, object[] parameters);

        private delegate void VoidMethodExecutor(object target, object[] parameters);

        public MethodInfo MethodInfo { get; }

        public ParameterInfo[] MethodParameters { get; }

        public TypeInfo TargetTypeInfo { get; }

        public Type AsyncResultType { get; }

        // This field is made internal set because it is set in unit tests.
        public Type MethodReturnType { get; internal set; }

        public bool IsMethodAsync { get; }

        public static ObjectMethodExecutor Create(MethodInfo methodInfo, TypeInfo targetTypeInfo)
        {
            return new ObjectMethodExecutor(methodInfo, targetTypeInfo, null);
        }

        public static ObjectMethodExecutor Create(MethodInfo methodInfo, TypeInfo targetTypeInfo, object[] parameterDefaultValues)
        {
            if (parameterDefaultValues == null)
            {
                throw new ArgumentNullException(nameof(parameterDefaultValues));
            }

            return new ObjectMethodExecutor(methodInfo, targetTypeInfo, parameterDefaultValues);
        }

        /// <summary>
        /// Executes the configured method on <paramref name="target"/>. This can be used whether or not
        /// the configured method is asynchronous.
        /// </summary>
        /// <remarks>
        /// Even if the target method is asynchronous, it's desirable to invoke it using Execute rather than
        /// ExecuteAsync if you know at compile time what the return type is, because then you can directly
        /// "await" that value (via a cast), and then the generated code will be able to reference the
        /// resulting awaitable as a value-typed variable. If you use ExecuteAsync instead, the generated
        /// code will have to treat the resulting awaitable as a boxed object, because it doesn't know at
        /// compile time what type it would be.
        /// </remarks>
        /// <param name="target">The object whose method is to be executed.</param>
        /// <param name="parameters">Parameters to pass to the method.</param>
        /// <returns>The method return value.</returns>
        public object Execute(object target, object[] parameters)
        {
            return _executor(target, parameters);
        }

        /// <summary>
        /// Executes the configured method on <paramref name="target"/>. This can only be used if the configured
        /// method is asynchronous.
        /// </summary>
        /// <remarks>
        /// If you don't know at compile time the type of the method's returned awaitable, you can use ExecuteAsync,
        /// which supplies an awaitable-of-object. This always works, but can incur several extra heap allocations
        /// as compared with using Execute and then using "await" on the result value typecasted to the known
        /// awaitable type. The possible extra heap allocations are for:
        /// 
        /// 1. The custom awaitable (though usually there's a heap allocation for this anyway, since normally
        ///    it's a reference type, and you normally create a new instance per call).
        /// 2. The custom awaiter (whether or not it's a value type, since if it's not, you need a new instance
        ///    of it, and if it is, it will have to be boxed so the calling code can reference it as an object).
        /// 3. The async result value, if it's a value type (it has to be boxed as an object, since the calling
        ///    code doesn't know what type it's going to be).
        /// </remarks>
        /// <param name="target">The object whose method is to be executed.</param>
        /// <param name="parameters">Parameters to pass to the method.</param>
        /// <returns>An object that you can "await" to get the method return value.</returns>
        public ObjectMethodExecutorAwaitable ExecuteAsync(object target, object[] parameters)
        {
            return _executorAsync(target, parameters);
        }

        public object GetDefaultValueForParameter(int index)
        {
            if (_parameterDefaultValues == null)
            {
                throw new InvalidOperationException($"Cannot call {nameof(GetDefaultValueForParameter)}, because no parameter default values were supplied.");
            }

            if (index < 0 || index > MethodParameters.Length - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _parameterDefaultValues[index];
        }

        private static MethodExecutor GetExecutor(MethodInfo methodInfo, TypeInfo targetTypeInfo)
        {
            // Parameters to executor
            var targetParameter = Expression.Parameter(typeof(object), "target");
            var parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

            // Build parameter list
            var parameters = new List<Expression>();
            var paramInfos = methodInfo.GetParameters();
            for (int i = 0; i < paramInfos.Length; i++)
            {
                var paramInfo = paramInfos[i];
                var valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                var valueCast = Expression.Convert(valueObj, paramInfo.ParameterType);

                // valueCast is "(Ti) parameters[i]"
                parameters.Add(valueCast);
            }

            // Call method
            var instanceCast = Expression.Convert(targetParameter, targetTypeInfo.AsType());
            var methodCall = Expression.Call(instanceCast, methodInfo, parameters);

            // methodCall is "((Ttarget) target) method((T0) parameters[0], (T1) parameters[1], ...)"
            // Create function
            if (methodCall.Type == typeof(void))
            {
                var lambda = Expression.Lambda<VoidMethodExecutor>(methodCall, targetParameter, parametersParameter);
                var voidExecutor = lambda.Compile();
                return WrapVoidMethod(voidExecutor);
            }
            else
            {
                // must coerce methodCall to match ActionExecutor signature
                var castMethodCall = Expression.Convert(methodCall, typeof(object));
                var lambda = Expression.Lambda<MethodExecutor>(castMethodCall, targetParameter, parametersParameter);
                return lambda.Compile();
            }
        }

        private static MethodExecutor WrapVoidMethod(VoidMethodExecutor executor)
        {
            return delegate (object target, object[] parameters)
            {
                executor(target, parameters);
                return null;
            };
        }

        private static MethodExecutorAsync GetExecutorAsync(
            MethodInfo methodInfo,
            TypeInfo targetTypeInfo,
            Expression coerceToAwaitableExpression,
            Type coerceToAwaitableType,
            MethodInfo getAwaiterMethod,
            Type awaiterType,
            Type awaiterResultType,
            PropertyInfo awaiterIsCompletedProperty,
            MethodInfo awaiterGetResultMethod,
            MethodInfo awaiterOnCompletedMethod,
            MethodInfo awaiterUnsafeOnCompletedMethod)
        {
            // Parameters to executor
            var targetParameter = Expression.Parameter(typeof(object), "target");
            var parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

            // Build parameter list
            var parameters = new List<Expression>();
            var paramInfos = methodInfo.GetParameters();
            for (int i = 0; i < paramInfos.Length; i++)
            {
                var paramInfo = paramInfos[i];
                var valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                var valueCast = Expression.Convert(valueObj, paramInfo.ParameterType);

                // valueCast is "(Ti) parameters[i]"
                parameters.Add(valueCast);
            }

            // Call method
            var instanceCast = Expression.Convert(targetParameter, targetTypeInfo.AsType());
            var methodCall = Expression.Call(instanceCast, methodInfo, parameters);

            // Using the method return value, construct an ObjectMethodExecutorAwaitable based on
            // the info we have about its implementation of the awaitable pattern. Note that all
            // the funcs/actions we construct here are precompiled, so that only one instance of
            // each is preserved throughout the lifetime of the ObjectMethodExecutor.

            // var getAwaiterFunc = (object awaitable) =>
            //     (object)((CustomAwaitableType)awaitable).GetAwaiter();
            var customAwaitableParam = Expression.Parameter(typeof(object), "awaitable");
            var postCoercionMethodReturnType = coerceToAwaitableType ?? methodInfo.ReturnType;
            var getAwaiterFunc = Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Call(
                        Expression.Convert(customAwaitableParam, postCoercionMethodReturnType),
                        getAwaiterMethod),
                    typeof(object)),
                customAwaitableParam).Compile();

            // var isCompletedFunc = (object awaiter) =>
            //     ((CustomAwaiterType)awaiter).IsCompleted;
            var isCompletedParam = Expression.Parameter(typeof(object), "awaiter");
            var isCompletedFunc = Expression.Lambda<Func<object, bool>>(
                Expression.MakeMemberAccess(
                    Expression.Convert(isCompletedParam, awaiterType),
                    awaiterIsCompletedProperty),
                isCompletedParam).Compile();

            var getResultParam = Expression.Parameter(typeof(object), "awaiter");
            Func<object, object> getResultFunc;
            if (awaiterResultType == typeof(void))
            {
                // var getResultFunc = (object awaiter) =>
                // {
                //     ((CustomAwaiterType)awaiter).GetResult(); // We need to invoke this to surface any exceptions
                //     return (object)null;
                // };
                getResultFunc = Expression.Lambda<Func<object, object>>(
                    Expression.Block(
                        Expression.Call(
                            Expression.Convert(getResultParam, awaiterType),
                            awaiterGetResultMethod),
                        Expression.Constant(null)
                    ),
                    getResultParam).Compile();
            }
            else
            {
                // var getResultFunc = (object awaiter) =>
                //     (object)((CustomAwaiterType)awaiter).GetResult();
                getResultFunc = Expression.Lambda<Func<object, object>>(
                    Expression.Convert(
                        Expression.Call(
                            Expression.Convert(getResultParam, awaiterType),
                            awaiterGetResultMethod),
                        typeof(object)),
                    getResultParam).Compile();
            }

            // var onCompletedFunc = (object awaiter, Action continuation) => {
            //     ((CustomAwaiterType)awaiter).OnCompleted(continuation);
            // };
            var onCompletedParam1 = Expression.Parameter(typeof(object), "awaiter");
            var onCompletedParam2 = Expression.Parameter(typeof(Action), "continuation");
            var onCompletedFunc = Expression.Lambda<Action<object, Action>>(
                Expression.Call(
                    Expression.Convert(onCompletedParam1, awaiterType),
                    awaiterOnCompletedMethod,
                    onCompletedParam2),
                onCompletedParam1,
                onCompletedParam2).Compile();

            Action<object, Action> unsafeOnCompletedFunc = null;
            if (awaiterUnsafeOnCompletedMethod != null)
            {
                // var unsafeOnCompletedFunc = (object awaiter, Action continuation) => {
                //     ((CustomAwaiterType)awaiter).UnsafeOnCompleted(continuation);
                // };
                var unsafeOnCompletedParam1 = Expression.Parameter(typeof(object), "awaiter");
                var unsafeOnCompletedParam2 = Expression.Parameter(typeof(Action), "continuation");
                unsafeOnCompletedFunc = Expression.Lambda<Action<object, Action>>(
                    Expression.Call(
                        Expression.Convert(unsafeOnCompletedParam1, awaiterType),
                        awaiterUnsafeOnCompletedMethod,
                        unsafeOnCompletedParam2),
                    unsafeOnCompletedParam1,
                    unsafeOnCompletedParam2).Compile();
            }

            // If we need to pass the method call result through a coercer function to get an
            // awaitable, then do so.
            var coercedMethodCall = coerceToAwaitableExpression != null
                ? Expression.Invoke(coerceToAwaitableExpression, methodCall)
                : (Expression)methodCall;

            // return new ObjectMethodExecutorAwaitable(
            //     coercedMethodCall,
            //     getAwaiterFunc,
            //     isCompletedFunc,
            //     getResultFunc,
            //     onCompletedFunc,
            //     unsafeOnCompletedFunc);
            var returnValueExpression = Expression.New(
                _objectMethodExecutorAwaitableConstructor,
                Expression.Convert(coercedMethodCall, typeof(object)),
                Expression.Constant(getAwaiterFunc),
                Expression.Constant(isCompletedFunc),
                Expression.Constant(getResultFunc),
                Expression.Constant(onCompletedFunc),
                Expression.Constant(unsafeOnCompletedFunc, typeof(Action<object, Action>)));

            var lambda = Expression.Lambda<MethodExecutorAsync>(returnValueExpression, targetParameter, parametersParameter);
            return lambda.Compile();
        }

        private static bool IsAwaitableDirectlyOrViaCoercion(
            Type type,
            out Expression coerceToAwaitableExpression,
            out Type coerceToAwaitableType,
            out MethodInfo getAwaiterMethod,
            out Type awaiterType,
            out Type returnType,
            out PropertyInfo isCompletedProperty,
            out MethodInfo getResultMethod,
            out MethodInfo onCompletedMethod,
            out MethodInfo unsafeOnCompletedMethod)
        {
            if (IsAwaitable(type,
                out getAwaiterMethod,
                out awaiterType,
                out returnType,
                out isCompletedProperty,
                out getResultMethod,
                out onCompletedMethod,
                out unsafeOnCompletedMethod))
            {
                coerceToAwaitableExpression = null;
                coerceToAwaitableType = null;
                return true;
            }

            // It's not directly awaitable, but maybe we can coerce it.
            // Currently we support coercing FSharpAsync<T>.
            if (ObjectMethodExecutorFSharpSupport.TryBuildCoercerFromFSharpAsyncToAwaitable(type,
                out coerceToAwaitableExpression,
                out coerceToAwaitableType))
            {
                return IsAwaitable(coerceToAwaitableType,
                    out getAwaiterMethod,
                    out awaiterType,
                    out returnType,
                    out isCompletedProperty,
                    out getResultMethod,
                    out onCompletedMethod,
                    out unsafeOnCompletedMethod);
            }

            return false;
        }

        private static bool IsAwaitable(
            Type type,
            out MethodInfo getAwaiterMethod,
            out Type awaiterType,
            out Type returnType,
            out PropertyInfo isCompletedProperty,
            out MethodInfo getResultMethod,
            out MethodInfo onCompletedMethod,
            out MethodInfo unsafeOnCompletedMethod)
        {
            // Based on Roslyn code: http://source.roslyn.io/#Microsoft.CodeAnalysis.Workspaces/Shared/Extensions/ISymbolExtensions.cs,db4d48ba694b9347

            // Awaitable must have method matching "object GetAwaiter()"
            getAwaiterMethod = type.GetRuntimeMethods().FirstOrDefault(m => 
                m.Name.Equals("GetAwaiter", StringComparison.OrdinalIgnoreCase)
                && m.GetParameters().Length == 0
                && m.ReturnType != null);
            if (getAwaiterMethod == null)
            {
                awaiterType = null;
                isCompletedProperty = null;
                onCompletedMethod = null;
                unsafeOnCompletedMethod = null;
                getResultMethod = null;
                returnType = null;
                return false;
            }

            awaiterType = getAwaiterMethod.ReturnType;

            // Awaiter must have property matching "bool IsCompleted { get; }"
            isCompletedProperty = awaiterType.GetRuntimeProperties().FirstOrDefault(p =>
                p.Name.Equals("IsCompleted", StringComparison.OrdinalIgnoreCase)
                && p.PropertyType == typeof(bool)
                && p.GetMethod != null);
            if (isCompletedProperty == null)
            {
                onCompletedMethod = null;
                unsafeOnCompletedMethod = null;
                getResultMethod = null;
                returnType = null;
                return false;
            }

            // Awaiter must implement INotifyCompletion
            var awaiterInterfaces = awaiterType.GetInterfaces();
            var implementsINotifyCompletion = awaiterInterfaces.Any(t => t == typeof(INotifyCompletion));
            if (implementsINotifyCompletion)
            {
                // INotifyCompletion supplies a method matching "void OnCompleted(Action action)"
                var interfaceMap = awaiterType.GetTypeInfo().GetRuntimeInterfaceMap(typeof(INotifyCompletion));
                onCompletedMethod = interfaceMap.InterfaceMethods.Single(m =>
                    m.Name.Equals("OnCompleted", StringComparison.OrdinalIgnoreCase)
                    && m.ReturnType == typeof(void)
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType == typeof(Action));
            }
            else
            {
                onCompletedMethod = null;
                unsafeOnCompletedMethod = null;
                getResultMethod = null;
                returnType = null;
                return false;
            }

            // Awaiter optionally implements ICriticalNotifyCompletion
            var implementsICriticalNotifyCompletion = awaiterInterfaces.Any(t => t == typeof(ICriticalNotifyCompletion));
            if (implementsICriticalNotifyCompletion)
            {
                // ICriticalNotifyCompletion supplies a method matching "void UnsafeOnCompleted(Action action)"
                var interfaceMap = awaiterType.GetTypeInfo().GetRuntimeInterfaceMap(typeof(ICriticalNotifyCompletion));
                unsafeOnCompletedMethod = interfaceMap.InterfaceMethods.Single(m =>
                    m.Name.Equals("UnsafeOnCompleted", StringComparison.OrdinalIgnoreCase)
                    && m.ReturnType == typeof(void)
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType == typeof(Action));
            }
            else
            {
                unsafeOnCompletedMethod = null;
            }

            // Awaiter must have method matching "void GetResult" or "T GetResult()"
            getResultMethod = awaiterType.GetRuntimeMethods().FirstOrDefault(m =>
                m.Name.Equals("GetResult")
                && m.GetParameters().Length == 0);
            if (getResultMethod == null)
            {
                returnType = null;
                return false;
            }

            returnType = getResultMethod.ReturnType;
            return true;
        }
    }
}
