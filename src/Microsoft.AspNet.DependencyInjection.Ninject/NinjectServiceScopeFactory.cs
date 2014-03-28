using System;
using System.Collections.Generic;
using System.Linq;
using Ninject;
using Ninject.Activation;
using Ninject.Parameters;
using Ninject.Syntax;

namespace Microsoft.AspNet.DependencyInjection.Ninject
{
    internal class NinjectServiceScopeFactory : IServiceScopeFactory
    {
        private readonly IResolutionRoot _resolver;
        private readonly IEnumerable<IParameter> _inheritedParameters;
        private readonly IServiceProvider _fallbackProvider;

        public NinjectServiceScopeFactory(IContext context)
        {
            _resolver = context.Kernel.Get<IResolutionRoot>();
            _inheritedParameters = context.Parameters.Where(p => p.ShouldInherit);

            var scopeParameter = _inheritedParameters.GetScopeParameter();
            if (scopeParameter != null)
            {
                _fallbackProvider = scopeParameter.FallbackProvider;
            }
        }

        public IServiceScope CreateScope()
        {
            return new NinjectServiceScope(_resolver, _inheritedParameters, _fallbackProvider);
        }

        private class NinjectServiceScope : IServiceScope
        {
            private readonly KScopeParameter _scope;
            private readonly IServiceProvider _serviceProvider;
            private readonly IServiceScope _fallbackScope;

            public NinjectServiceScope(
                IResolutionRoot resolver,
                IEnumerable<IParameter> inheritedParameters,
                IServiceProvider parentFallbackProvider)
            {
                if (parentFallbackProvider != null)
                {
                    var scopeFactory = parentFallbackProvider.GetServiceOrDefault<IServiceScopeFactory>();
                    if (scopeFactory != null)
                    {
                        _fallbackScope = scopeFactory.CreateScope();
                        _scope = new KScopeParameter(_fallbackScope.ServiceProvider);
                    }
                }

                if (_scope == null)
                {
                    _scope = new KScopeParameter(parentFallbackProvider);
                }

                inheritedParameters = inheritedParameters.AddOrReplaceScopeParameter(_scope);
                _serviceProvider = new NinjectServiceProvider(resolver, inheritedParameters.ToArray());
            }

            public IServiceProvider ServiceProvider
            {
                get { return _serviceProvider; }
            }

            public void Dispose()
            {
                _scope.Dispose();

                if (_fallbackScope != null)
                {
                    _fallbackScope.Dispose();
                }
            }
        }
    }
}
