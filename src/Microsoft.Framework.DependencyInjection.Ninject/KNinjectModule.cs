// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
