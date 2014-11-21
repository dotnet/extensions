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
        public static void Populate(this IKernel kernel, IEnumerable<IServiceDescriptor> descriptors)
        {
            kernel.Load(new KNinjectModule(descriptors));
        }

        public static IBindingNamedWithOrOnSyntax<T> InKScope<T>(
                this IBindingWhenInNamedWithOrOnSyntax<T> binding)
        {
            return binding.InScope(context => context.Parameters.GetScopeParameter());
        }

        internal static KScopeParameter GetScopeParameter(this IEnumerable<IParameter> parameters)
        {
            return (KScopeParameter)(parameters
                .Where(p => p.Name == typeof(KScopeParameter).FullName)
                .SingleOrDefault());
        }

        internal static IEnumerable<IParameter> AddOrReplaceScopeParameter(
                this IEnumerable<IParameter> parameters,
                KScopeParameter scopeParameter)
        {
            return parameters
                .Where(p => p.Name != typeof(KScopeParameter).FullName)
                .Concat(new[] { scopeParameter });
        }
    }
}
