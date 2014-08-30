// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Ninject;
using Ninject.Modules;
using Ninject.Planning.Bindings.Resolvers;
using Ninject.Syntax;

namespace Microsoft.Framework.DependencyInjection.Ninject
{
    internal class KNinjectModule : NinjectModule
    {
        private readonly IEnumerable<IServiceDescriptor> _serviceDescriptors;
        private readonly IServiceProvider _fallbackProvider;

        public KNinjectModule(
                IEnumerable<IServiceDescriptor> serviceDescriptors,
                IServiceProvider fallbackProvider)
        {
            _serviceDescriptors = serviceDescriptors;
            _fallbackProvider = fallbackProvider;
        }

        public override void Load()
        {
            foreach (var descriptor in _serviceDescriptors)
            {
                IBindingWhenInNamedWithOrOnSyntax<object> binding;

                if (descriptor.ImplementationType != null)
                {
                    binding = Bind(descriptor.ServiceType).To(descriptor.ImplementationType);
                }
                else if (descriptor.ImplementationFactory != null)
                {
                    binding = Bind(descriptor.ServiceType).ToMethod(context =>
                    {
                        var serviceProvider = context.Kernel.Get<IServiceProvider>();
                        return descriptor.ImplementationFactory(serviceProvider);
                    });
                }
                else
                {
                    binding = Bind(descriptor.ServiceType).ToConstant(descriptor.ImplementationInstance);
                }

                switch (descriptor.Lifecycle)
                {
                    case LifecycleKind.Singleton:
                        binding.InSingletonScope();
                        break;
                    case LifecycleKind.Scoped:
                        binding.InKScope();
                        break;
                    case LifecycleKind.Transient:
                        binding.InTransientScope();
                        break;
                }
            }

            Bind<IServiceProvider>().ToMethod(context =>
            {
                var resolver = context.Kernel.Get<IResolutionRoot>();
                var inheritedParams = context.Parameters.Where(p => p.ShouldInherit);

                var scopeParam = new KScopeParameter(_fallbackProvider);
                inheritedParams = inheritedParams.AddOrReplaceScopeParameter(scopeParam);

                return new NinjectServiceProvider(resolver, inheritedParams.ToArray());
            }).InKScope();

            Bind<IServiceScopeFactory>().ToMethod(context =>
            {
                return new NinjectServiceScopeFactory(context);
            }).InKScope();

            if (_fallbackProvider != null)
            {
                Kernel.Components.Add<IMissingBindingResolver, ChainedMissingBindingResolver>();
            }
        }
    }
}
