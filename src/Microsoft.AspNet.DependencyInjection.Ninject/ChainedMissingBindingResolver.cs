using System;
using System.Collections.Generic;
using Ninject.Activation;
using Ninject.Components;
using Ninject.Infrastructure;
using Ninject.Parameters;
using Ninject.Planning.Bindings;
using Ninject.Planning.Bindings.Resolvers;

namespace Microsoft.AspNet.DependencyInjection.Ninject
{
    internal class ChainedMissingBindingResolver : NinjectComponent, IMissingBindingResolver
    {
        public IEnumerable<IBinding> Resolve(Multimap<Type, IBinding> bindings, IRequest request)
        {
            var fallbackProvider = GetFallbackProvider(request.Parameters);
            if (fallbackProvider != null && fallbackProvider.HasService(request.Service))
            {
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
