// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Ninject;
using Ninject.Activation;
using Ninject.Parameters;
using Ninject.Syntax;

namespace Microsoft.Framework.DependencyInjection.Ninject
{
    internal class NinjectServiceScopeFactory : IServiceScopeFactory
    {
        private readonly IResolutionRoot _resolver;
        private readonly IEnumerable<IParameter> _inheritedParameters;

        public NinjectServiceScopeFactory(IContext context)
        {
            _resolver = context.Kernel.Get<IResolutionRoot>();
            _inheritedParameters = context.Parameters.Where(p => p.ShouldInherit);
        }

        public IServiceScope CreateScope()
        {
            return new NinjectServiceScope(_resolver, _inheritedParameters);
        }

        private class NinjectServiceScope : IServiceScope
        {
            private readonly KScopeParameter _scope;
            private readonly IServiceProvider _serviceProvider;

            public NinjectServiceScope(
                IResolutionRoot resolver,
                IEnumerable<IParameter> inheritedParameters)
            {
                _scope = new KScopeParameter();
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
            }
        }
    }
}
