// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ninject.Activation;
using Ninject.Components;
using Ninject.Infrastructure;
using Ninject.Parameters;
using Ninject.Planning.Bindings;
using Ninject.Planning.Bindings.Resolvers;

namespace Microsoft.Framework.DependencyInjection.Ninject
{
    internal class ChainedMissingBindingResolver : NinjectComponent, IMissingBindingResolver
    {
        public IEnumerable<IBinding> Resolve(Multimap<Type, IBinding> bindings, IRequest request)
        {
            IServiceProvider fallbackProvider = GetFallbackProvider(request.Parameters);
            if (fallbackProvider == null)
            {
                yield break;
            }

            object fallbackService = fallbackProvider.GetServiceOrNull(request.Service);
            if (fallbackService == null)
            {
                yield break;
            }

            // The fallback provider shouldn't be responsible for resolving the service
            // if all it provides is an empty IEnumerable
            var fallbackEnumerable = fallbackService as IEnumerable;
            if (fallbackEnumerable != null && !fallbackEnumerable.GetEnumerator().MoveNext())
            {
                yield break;
            }

            // The fallback provider shouldn't be responsible for resolving an IEnumerable<T> service
            // if Ninject already has any services registered for T
            if (fallbackEnumerable != null)
            {
                var collectionTypeInfo = request.Service.GetTypeInfo();
                if (collectionTypeInfo.IsGenericType &&
                    collectionTypeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    var itemType = collectionTypeInfo.GenericTypeArguments.Single();
                    if (bindings.ContainsKey(itemType))
                    {
                        yield break;
                    }
                }
            }

            yield return new Binding(request.Service)
            {
                ProviderCallback = context =>
                {
                    return new ChainedProvider(
                        context.Request.Service,
                        GetFallbackProvider(context.Request.Parameters));
                }
            };
        }

        private static IServiceProvider GetFallbackProvider(IEnumerable<IParameter> parameters)
        {
            var scopeParameter = parameters.GetScopeParameter();
            if (scopeParameter != null)
            {
                return scopeParameter.FallbackProvider;
            }
            else
            {
                return null;
            }
        }

        private class ChainedProvider : IProvider
        {
            private readonly IServiceProvider _fallbackProvider;

            public ChainedProvider(Type serviceType, IServiceProvider fallbackProvider)
            {
                Type = serviceType;
                _fallbackProvider = fallbackProvider;
            }

            public Type Type { get; private set; }

            public object Create(IContext context)
            {
                return _fallbackProvider.GetService(Type);
            }
        }
    }
}
