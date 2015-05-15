// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || DNX451 || DNXCORE50

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.Framework.Notification.Internal
{
    public static class ProxyTypeEmitter
    {
        public static Type GetProxyType(ProxyTypeCache cache, Type targetType, Type sourceType)
        {
            if (targetType.IsAssignableFrom(sourceType))
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
                    throw new InvalidOperationException(error.Error);
                }

                Debug.Assert(context.Visited.ContainsKey(context.Key));

                // Now that we've generated all of the constructors for the proxies, we can generate the rest
                // of the type.
                foreach (var verificationResult in context.Visited)
                {
                    AddProperties(
                        context,
                        verificationResult.Value.TypeBuilder,
                        verificationResult.Value.Mappings);


                    verificationResult.Value.TypeBuilder.CreateTypeInfo().AsType();
                }

                // We only want to publish the results after all of the proxies are totally generated.
                foreach (var verificationResult in context.Visited)
                {
                    cache[verificationResult.Key] = ProxyTypeCacheResult.FromTypeBuilder(
                        verificationResult.Key, 
                        verificationResult.Value.TypeBuilder,
                        verificationResult.Value.ConstructorBuilder);
                }

                return context.Visited[context.Key].TypeBuilder.CreateTypeInfo().AsType();
            }
            else if (result.IsError)
            {
                throw new InvalidOperationException(result.Error);
            }
            else if (result.TypeBuilder == null)
            {
                // This is an identity convertion
                return null;
            }
            else
            {
                return result.TypeBuilder.CreateTypeInfo().AsType();
            }
        }

        private static bool VerifyProxySupport(ProxyBuilderContext context, Tuple<Type, Type> key)
        {
            var sourceType = key.Item1;
            var targetType = key.Item2;

            if (context.Visited.ContainsKey(key))
            {
                // We've already seen this combination and so far so good.
                return true;
            }

            ProxyTypeCacheResult cacheResult;
            if (context.Cache.TryGetValue(key, out cacheResult))
            {
                // If we get here we've got a published conversion or error, so we can stop searching.
                return !cacheResult.IsError;
            }

            if (targetType == sourceType || targetType.IsAssignableFrom(sourceType))
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
            var verificationResult = new VerificationResult();
            context.Visited.Add(key, verificationResult);

            var propertyMappings = new List<KeyValuePair<PropertyInfo, PropertyInfo>>();

            var sourceProperties = sourceType.GetRuntimeProperties();
            foreach (var targetProperty in targetType.GetRuntimeProperties())
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
            il.Emit(OpCodes.Call, baseType.GetConstructor(new Type[] { sourceType }));
            il.Emit(OpCodes.Ret);

            verificationResult.ConstructorBuilder = constructorBuilder;
            verificationResult.TypeBuilder = typeBuilder;

            return true;
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
                    property.Key.PropertyType,
                    Type.EmptyTypes);

                var methodBuilder = typeBuilder.DefineMethod(
                    targetProperty.GetMethod.Name,
                    targetProperty.GetMethod.Attributes & ~MethodAttributes.Abstract,
                    targetProperty.GetMethod.CallingConvention,
                    targetProperty.GetMethod.ReturnType,
                    Type.EmptyTypes);
                propertyBuilder.SetGetMethod(methodBuilder);
                typeBuilder.DefineMethodOverride(methodBuilder, targetProperty.GetMethod);

                var il = methodBuilder.GetILGenerator();
                if (sourceProperty == null)
                {
                    il.DeclareLocal(targetProperty.PropertyType);

                    // Return a default(T) value.
                    il.Emit(OpCodes.Ldloca_S, 0);
                    il.Emit(OpCodes.Initobj, targetProperty.PropertyType);

                    il.Emit(OpCodes.Ldloc_S, 0);
                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    // Push 'this' and get the underlying instance.
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, 
                        typeBuilder.BaseType.GetField(
                            "Instance",
                            BindingFlags.Instance | BindingFlags.Public));

                    // Call the source property.
                    il.EmitCall(OpCodes.Callvirt, sourceProperty.GetMethod, null);

                    // Create a proxy for the value returned by source property (if necessary).
                    EmitProxy(context, il, targetProperty.PropertyType, sourceProperty.PropertyType);
                    il.Emit(OpCodes.Ret);
                }
            }
        }

        private static void EmitProxy(ProxyBuilderContext context, ILGenerator il, Type targetType, Type sourceType)
        {
            if (sourceType == targetType)
            {
                // Do nothing.
                return;
            }
            else if (targetType.IsAssignableFrom(sourceType))
            {
                il.Emit(OpCodes.Castclass, targetType);
                return;
            }

            // If we get here, then we actually need a proxy.
            var key = new Tuple<Type, Type>(sourceType, targetType);

            ConstructorBuilder constructorBuilder = null;
            ProxyTypeCacheResult cacheResult;
            VerificationResult verificationResult;
            if (context.Cache.TryGetValue(key, out cacheResult))
            {
                Debug.Assert(!cacheResult.IsError);
                Debug.Assert(cacheResult.ConstructorBuilder != null);

                // This means we've got a fully-built (published) type.
                constructorBuilder = cacheResult.ConstructorBuilder;
            }
            else if (context.Visited.TryGetValue(key, out verificationResult))
            {
                Debug.Assert(verificationResult.ConstructorBuilder != null);
                constructorBuilder = verificationResult.ConstructorBuilder;
            }

            Debug.Assert(constructorBuilder != null);

            var endLabel = il.DefineLabel();
            var createProxyLabel = il.DefineLabel();

            // If the 'source' value is null, then just return it.
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brfalse_S, endLabel);

            // If the 'source' value isn't a proxy then we need to create one.
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Isinst, typeof(ProxyBase));
            il.Emit(OpCodes.Brfalse_S, createProxyLabel);

            // If the 'source' value is-a proxy then get the wrapped value.
            il.Emit(OpCodes.Isinst, typeof(ProxyBase));
            il.EmitCall(OpCodes.Callvirt, typeof(ProxyBase).GetMethod("get_UnderlyingInstanceAsObject"), null);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Isinst, targetType);
            il.Emit(OpCodes.Brtrue_S, endLabel);

            il.MarkLabel(createProxyLabel);

            // Create the proxy.
            il.Emit(OpCodes.Newobj, constructorBuilder);

            il.MarkLabel(endLabel);
        }

        private class ProxyBuilderContext
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

        private class VerificationResult
        {
            public ConstructorBuilder ConstructorBuilder { get; set; }

            public IEnumerable<KeyValuePair<PropertyInfo, PropertyInfo>> Mappings { get; set; }

            public TypeBuilder TypeBuilder { get; set; }
        }
    }
}
#endif
