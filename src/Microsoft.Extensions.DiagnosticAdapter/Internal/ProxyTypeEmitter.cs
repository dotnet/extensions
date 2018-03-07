// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP2_0 || NET461
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.Extensions.DiagnosticAdapter.Internal
{
    public static class ProxyTypeEmitter
    {
        private static readonly Type[] EmptyTypes = new Type[0];
        private static object _lock = new object();

        public static Type GetProxyType(ProxyTypeCache cache, Type targetType, Type sourceType)
        {
            if (targetType.GetTypeInfo().IsAssignableFrom(sourceType.GetTypeInfo()))
            {
                return null;
            }

            var key = new Tuple<Type, Type>(sourceType, targetType);

            ProxyTypeCacheResult result;
            if (!cache.TryGetValue(key, out result))
            {
                var context = new ProxyBuilderContext(cache, targetType, sourceType);

                // Check that all required types are proxy-able - this will create the TypeBuilder, Constructor,
                // and property mappings.
                //
                // We need to create the TypeBuilder and Constructor up front to deal with cycles that can occur
                // when generating the proxy properties.
                if (!VerifyProxySupport(context, context.Key))
                {
                    var error = cache[key];
                    Debug.Assert(error != null && error.IsError);
                    throw new InvalidProxyOperationException(error.Error);
                }

                Debug.Assert(context.Visited.ContainsKey(context.Key));

                // Now that we've generated all of the constructors for the proxies, we can generate the properties.
                foreach (var verificationResult in context.Visited)
                {
                    if (verificationResult.Value.Mappings != null)
                    {
                        AddProperties(
                            context,
                            verificationResult.Value.TypeBuilder,
                            verificationResult.Value.Mappings);
                    }
                }

                // Now generate the type
                foreach (var verificationResult in context.Visited)
                {
                    if (verificationResult.Value.TypeBuilder != null)
                    {
                        verificationResult.Value.Type = verificationResult.Value.TypeBuilder.CreateTypeInfo().AsType();
                    }
                }

                // We only want to publish the results after all of the proxies are totally generated.
                foreach (var verificationResult in context.Visited)
                {
                    cache.TryAdd(verificationResult.Key, ProxyTypeCacheResult.FromType(
                        verificationResult.Key,
                        verificationResult.Value.Type,
                        verificationResult.Value.Constructor));
                }

                return context.Visited[context.Key].Type;
            }
            else if (result.IsError)
            {
                throw new InvalidOperationException(result.Error);
            }
            else if (result.Type == null)
            {
                // This is an identity conversion
                return null;
            }
            else
            {
                return result.Type;
            }
        }

        internal static bool VerifyProxySupport(ProxyBuilderContext context, Tuple<Type, Type> key)
        {
            var sourceType = key.Item1;
            var targetType = key.Item2;

            if (context.Visited.ContainsKey(key))
            {
                // We've already seen this combination and so far so good.
                return true;
            }

            ProxyTypeCacheResult cacheResult;
            var verificationResult = new VerificationResult();
            if (context.Cache.TryGetValue(key, out cacheResult))
            {
                // There may be a possible race condition, which adds the type we are generating to the cache
                // before we verify it. This ensures that the result is stored in context.Visited in that scenario.
                verificationResult.Constructor = cacheResult.Constructor;
                verificationResult.Type = cacheResult.Type;
                context.Visited.Add(key, verificationResult);

                // If we get here we've got a published conversion or error, so we can stop searching.
                return !cacheResult.IsError;
            }

            if (targetType == sourceType || targetType.GetTypeInfo().IsAssignableFrom(sourceType.GetTypeInfo()))
            {
                // If we find a trivial conversion, then that will work.
                return true;
            }

            if (!targetType.GetTypeInfo().IsInterface)
            {
                var message = Resources.FormatConverter_TypeMustBeInterface(targetType.FullName, sourceType.FullName);
                context.Cache[key] = ProxyTypeCacheResult.FromError(key, message);

                return false;
            }

            // This is a combination we haven't seen before, and it *might* support proxy generation, so let's
            // start trying.
            context.Visited.Add(key, verificationResult);

            // We support conversions from IList<T> -> IReadOnlyList<U> and IReadOnlyList<T> -> IReadOnlyList<U>
            if (targetType.GetTypeInfo().IsGenericType &&
                targetType.GetTypeInfo().GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
            {
                var sourceInterfaceType = GetGenericImplementation(sourceType, typeof(IList<>));
                if (sourceInterfaceType != null)
                {
                    var targetElementType = targetType.GetTypeInfo().GenericTypeArguments[0];
                    var sourceElementType = sourceInterfaceType.GetTypeInfo().GenericTypeArguments[0];

                    var elementKey = new Tuple<Type, Type>(sourceElementType, targetElementType);
                    if (!VerifyProxySupport(context, elementKey))
                    {
                        var error = context.Cache[elementKey];
                        Debug.Assert(error != null && error.IsError);
                        context.Cache[key] = error;
                        return false;
                    }

                    VerificationResult elementResult;
                    context.Visited.TryGetValue(elementKey, out elementResult);

                    var proxyType = elementResult?.Type ?? (Type)elementResult?.TypeBuilder;
                    if (proxyType == null)
                    {
                        // No proxy needed for elements.
                        verificationResult.Type = typeof(ProxyList<,>).MakeGenericType(elementKey.Item1, elementKey.Item2);
                        verificationResult.Constructor = verificationResult.Type.GetTypeInfo().DeclaredConstructors.First();
                    }
                    else
                    {
                        lock (_lock)
                        {
                            // We need to proxy each of the elements. Let's generate a type.
                            GenerateProxyTypeForList(elementKey.Item1, elementKey.Item2, proxyType, verificationResult);
                        }
                    }

                    return true;
                }
            }

            // This doesn't match any of our interface conversions, so we'll codegen a proxy.
            var propertyMappings = new List<KeyValuePair<PropertyInfo, PropertyInfo>>();

            var sourceProperties = sourceType.GetRuntimeProperties();
            var targetProperties = targetType
                .GetTypeInfo()
                .ImplementedInterfaces
                .SelectMany(i => i.GetRuntimeProperties())
                .Concat(targetType.GetRuntimeProperties());
            foreach (var targetProperty in targetProperties)
            {
                if (!targetProperty.CanRead)
                {
                    var message = Resources.FormatConverter_PropertyMustHaveGetter(
                        targetProperty.Name,
                        targetType.FullName);
                    context.Cache[key] = ProxyTypeCacheResult.FromError(key, message);

                    return false;
                }

                if (targetProperty.CanWrite)
                {
                    var message = Resources.FormatConverter_PropertyMustNotHaveSetter(
                        targetProperty.Name,
                        targetType.FullName);
                    context.Cache[key] = ProxyTypeCacheResult.FromError(key, message);

                    return false;
                }

                if (targetProperty.GetIndexParameters()?.Length > 0)
                {
                    var message = Resources.FormatConverter_PropertyMustNotHaveIndexParameters(
                        targetProperty.Name,
                        targetType.FullName);
                    context.Cache[key] = ProxyTypeCacheResult.FromError(key, message);

                    return false;
                }

                // To allow for flexible versioning, we want to allow missing properties in the source.
                //
                // For now we'll just store null, and later generate a stub getter that returns default(T).
                var sourceProperty = sourceProperties.Where(p => p.Name == targetProperty.Name).FirstOrDefault();
                if (sourceProperty != null &&
                    sourceProperty.CanRead &&
                    sourceProperty.GetMethod?.IsPublic == true)
                {
                    var propertyKey = new Tuple<Type, Type>(sourceProperty.PropertyType, targetProperty.PropertyType);
                    if (!VerifyProxySupport(context, propertyKey))
                    {
                        // There's an error here, so bubble it up and cache it.
                        var error = context.Cache[propertyKey];
                        Debug.Assert(error != null && error.IsError);

                        context.Cache[key] = ProxyTypeCacheResult.FromError(key, error.Error);
                        return false;
                    }

                    propertyMappings.Add(new KeyValuePair<PropertyInfo, PropertyInfo>(targetProperty, sourceProperty));
                }
                else
                {
                    propertyMappings.Add(new KeyValuePair<PropertyInfo, PropertyInfo>(targetProperty, null));
                }
            }

            verificationResult.Mappings = propertyMappings;
            lock (_lock)
            {
                GenerateProxyTypeFromProperties(sourceType, targetType, verificationResult);
            }

            return true;
        }

        private static void GenerateProxyTypeForList(
            Type sourceElementType,
            Type targetElementType,
            Type proxyType,
            VerificationResult verificationResult)
        {
            var baseType = typeof(ProxyList<,>).MakeGenericType(sourceElementType, targetElementType);
            var baseConstructor = baseType.GetTypeInfo().DeclaredConstructors.FirstOrDefault(ctor => !ctor.IsPublic && !ctor.IsStatic);

            var typeBuilder = ProxyAssembly.DefineType(
                string.Format("Proxy_From_IList<{0}>_To_IReadOnlyList<{1}>", sourceElementType.Name, targetElementType.Name),
                TypeAttributes.Class,
                baseType,
                new Type[] { typeof(IReadOnlyList<>).MakeGenericType(targetElementType) });

            var constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new Type[] { typeof(IList<>).MakeGenericType(sourceElementType) });

            var il = constructorBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);

            // LdToken loads a RuntimeTypeHandle, while the constructor takes a Type, so we convert.
            // This is the same strategy the compiler uses when constructing this class.
            il.Emit(OpCodes.Ldtoken, proxyType);
            var getTypeFromHandle = typeof(Type).GetRuntimeMethods()
                .First(m => string.Equals(m.Name, nameof(Type.GetTypeFromHandle)) && m.IsStatic && m.IsPublic);
            il.EmitCall(OpCodes.Call, getTypeFromHandle, null);

            il.Emit(OpCodes.Call, baseConstructor);
            il.Emit(OpCodes.Ret);

            verificationResult.Constructor = constructorBuilder;
            verificationResult.TypeBuilder = typeBuilder;
        }

        private static void GenerateProxyTypeFromProperties(Type sourceType, Type targetType, VerificationResult verificationResult)
        {
            var baseType = typeof(ProxyBase<>).MakeGenericType(sourceType);
            var typeBuilder = ProxyAssembly.DefineType(
                string.Format("Proxy_From_{0}_To_{1}", sourceType.Name, targetType.Name),
                TypeAttributes.Class,
                baseType,
                new Type[] { targetType });

            var constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new Type[] { sourceType });

            var il = constructorBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, sourceType);
            il.Emit(OpCodes.Call, baseType.GetTypeInfo()
                .DeclaredConstructors
                .First(c =>
                {
                    var parameters = c.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType == sourceType;
                }));
            il.Emit(OpCodes.Ret);

            verificationResult.Constructor = constructorBuilder;
            verificationResult.TypeBuilder = typeBuilder;
        }

        private static void AddProperties(
            ProxyBuilderContext context,
            TypeBuilder typeBuilder,
            IEnumerable<KeyValuePair<PropertyInfo, PropertyInfo>> properties)
        {
            foreach (var property in properties)
            {
                var targetProperty = property.Key;
                var sourceProperty = property.Value;

                var propertyBuilder = typeBuilder.DefineProperty(
                    targetProperty.Name,
                    PropertyAttributes.None,
                    targetProperty.PropertyType,
                    EmptyTypes);

                var methodBuilder = typeBuilder.DefineMethod(
                    targetProperty.GetMethod.Name,
                    targetProperty.GetMethod.Attributes & ~MethodAttributes.Abstract,
                    targetProperty.GetMethod.CallingConvention,
                    targetProperty.GetMethod.ReturnType,
                    EmptyTypes);
                propertyBuilder.SetGetMethod(methodBuilder);
                typeBuilder.DefineMethodOverride(methodBuilder, targetProperty.GetMethod);

                var il = methodBuilder.GetILGenerator();
                if (sourceProperty == null)
                {
                    // Return a default(T) value.
                    il.DeclareLocal(targetProperty.PropertyType);

                    il.Emit(OpCodes.Ldloca_S, 0);
                    il.Emit(OpCodes.Initobj, targetProperty.PropertyType);

                    il.Emit(OpCodes.Ldloc_S, 0);
                    il.Emit(OpCodes.Ret);
                    continue;
                }

                il.DeclareLocal(targetProperty.PropertyType);
                il.DeclareLocal(sourceProperty.PropertyType);

                // Init variables with default(T)
                il.Emit(OpCodes.Ldloca_S, 0);
                il.Emit(OpCodes.Initobj, targetProperty.PropertyType);

                il.Emit(OpCodes.Ldloca_S, 1);
                il.Emit(OpCodes.Initobj, sourceProperty.PropertyType);

                // Push 'this' and get the underlying instance.
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld,
                    typeBuilder.BaseType.GetRuntimeFields().First(
                        f => string.Equals(f.Name, "Instance", StringComparison.Ordinal) &&
                            !f.IsStatic &&
                            f.IsPublic));

                // Call the source property.
                il.EmitCall(OpCodes.Callvirt, sourceProperty.GetMethod, null);
                il.Emit(OpCodes.Stloc_S, 1);

                var @return = il.DefineLabel();

                // BrFalse can't handle all value types (Decimal for example), so we skip it for value types
                // since we're using it as a null-check anyway.
                if (!sourceProperty.PropertyType.GetTypeInfo().IsValueType)
                {
                    // At this point the value on the stack is the return value of the the source-type property.
                    // If it returned null, we want to just jump to return.
                    il.Emit(OpCodes.Ldloc_S, 1);
                    il.Emit(OpCodes.Brfalse, @return);
                }

                // Create a proxy for the value returned by source property (if necessary).
                il.Emit(OpCodes.Ldloc_S, 1);
                EmitProxy(context, il, targetProperty.PropertyType, sourceProperty.PropertyType);
                il.Emit(OpCodes.Stloc_S, 0);

                il.MarkLabel(@return);
                il.Emit(OpCodes.Ldloc_S, 0);
                il.Emit(OpCodes.Ret);
            }
        }

        private static void EmitProxy(ProxyBuilderContext context, ILGenerator il, Type targetType, Type sourceType)
        {
            if (sourceType == targetType)
            {
                // Do nothing.
                return;
            }
            else if (targetType.GetTypeInfo().IsAssignableFrom(sourceType.GetTypeInfo()))
            {
                il.Emit(OpCodes.Castclass, targetType);
                return;
            }

            // If we get here, then we actually need a proxy.
            var key = new Tuple<Type, Type>(sourceType, targetType);

            ConstructorInfo constructor = null;
            ProxyTypeCacheResult cacheResult;
            VerificationResult verificationResult;
            if (context.Cache.TryGetValue(key, out cacheResult))
            {
                Debug.Assert(!cacheResult.IsError);
                Debug.Assert(cacheResult.Constructor != null);

                // This means we've got a fully-built (published) type.
                constructor = cacheResult.Constructor;
            }
            else if (context.Visited.TryGetValue(key, out verificationResult))
            {
                if (verificationResult.Constructor != null)
                {
                    constructor = verificationResult.Constructor;
                }
            }

            Debug.Assert(constructor != null);

            // Create the proxy.
            il.Emit(OpCodes.Newobj, constructor);
        }

        private static Type GetGenericImplementation(Type type, Type openGenericInterfaceType)
        {
            if (type.GetTypeInfo().IsGenericType &&
                type.GetTypeInfo().GetGenericTypeDefinition() == openGenericInterfaceType)
            {
                return type;
            }

            foreach (var interfaceType in type.GetTypeInfo().ImplementedInterfaces)
            {
                if (interfaceType.GetTypeInfo().IsGenericType &&
                    interfaceType.GetTypeInfo().GetGenericTypeDefinition() == openGenericInterfaceType)
                {
                    return interfaceType;
                }
            }

            return null;
        }

        internal class ProxyBuilderContext
        {
            public ProxyBuilderContext(ProxyTypeCache cache, Type targetType, Type sourceType)
            {
                Cache = cache;

                Key = new Tuple<Type, Type>(sourceType, targetType);
                Visited = new Dictionary<Tuple<Type, Type>, VerificationResult>();
            }

            public ProxyTypeCache Cache { get; }

            public Tuple<Type, Type> Key { get; }

            public Type SourceType => Key.Item1;

            public Type TargetType => Key.Item2;

            public Dictionary<Tuple<Type, Type>, VerificationResult> Visited { get; }
        }

        internal class VerificationResult
        {
            public ConstructorInfo Constructor { get; set; }

            public IEnumerable<KeyValuePair<PropertyInfo, PropertyInfo>> Mappings { get; set; }

            public TypeBuilder TypeBuilder { get; set; }

            public Type Type { get; set; }
        }
    }
}
#elif NETSTANDARD2_0
#else
#error Target frameworks should be updated
#endif
