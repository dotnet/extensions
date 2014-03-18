using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.DependencyInjection.Fallback
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceProvider BuildServiceProvider(
                this IEnumerable<IServiceDescriptor> collection)
        {
            return BuildServiceProvider(collection, fallbackServices: null);
        }

        public static IServiceProvider BuildServiceProvider(
                this IEnumerable<IServiceDescriptor> collection,
                IServiceProvider fallbackServices)
        {
            return new ServiceProvider(fallbackServices).Add(collection);
        }
    }
}
