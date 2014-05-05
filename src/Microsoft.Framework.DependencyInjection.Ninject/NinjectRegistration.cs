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
using Ninject.Parameters;
using Ninject.Syntax;

namespace Microsoft.Framework.DependencyInjection.Ninject
{
    public static class NinjectRegistration
    {
        public static void Populate(IKernel kernel, IEnumerable<IServiceDescriptor> descriptors)
        {
            Populate(kernel, descriptors, fallbackServiceProvider: null);
        }

        public static void Populate(
                IKernel kernel,
                IEnumerable<IServiceDescriptor> descriptors,
                IServiceProvider fallbackServiceProvider)
        {
            kernel.Load(new KNinjectModule(descriptors, fallbackServiceProvider));
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
