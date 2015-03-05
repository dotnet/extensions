// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Ninject;
using Ninject.Parameters;
using Ninject.Syntax;

namespace Microsoft.Framework.DependencyInjection.Ninject
{
    public static class NinjectRegistration
    {
        public static void Populate(this IKernel kernel, IEnumerable<ServiceDescriptor> descriptors)
        {
            kernel.Load(new ServiceProviderNinjectModule(descriptors));
        }

        public static IBindingNamedWithOrOnSyntax<T> InRequestScope<T>(
                this IBindingWhenInNamedWithOrOnSyntax<T> binding)
        {
            return binding.InScope(context => context.Parameters.GetScopeParameter());
        }

        internal static ScopeParameter GetScopeParameter(this IEnumerable<IParameter> parameters)
        {
            return (ScopeParameter)(parameters
                .Where(p => p.Name == typeof(ScopeParameter).FullName)
                .SingleOrDefault());
        }

        internal static IEnumerable<IParameter> AddOrReplaceScopeParameter(
                this IEnumerable<IParameter> parameters,
                ScopeParameter scopeParameter)
        {
            return parameters
                .Where(p => p.Name != typeof(ScopeParameter).FullName)
                .Concat(new[] { scopeParameter });
        }
    }
}
