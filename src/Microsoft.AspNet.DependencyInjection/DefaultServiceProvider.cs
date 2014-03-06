using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.DependencyInjection
{
    public static class DefaultServiceProvider
    {
        public static IServiceProvider Create(
                IServiceProvider wrappedProvider,
                IEnumerable<IServiceDescriptor> descriptors)
        {
            var provider = new ServiceProvider(wrappedProvider);
            return provider.Add(descriptors);
        }
    }
}
