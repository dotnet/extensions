using System;
using System.Collections.Generic;
using System.Linq;
using Ninject;
using Ninject.Parameters;
using Ninject.Syntax;

namespace Microsoft.AspNet.DependencyInjection.Ninject
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
