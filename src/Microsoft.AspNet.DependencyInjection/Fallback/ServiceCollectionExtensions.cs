using System;

namespace Microsoft.AspNet.DependencyInjection.Fallback
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceProvider BuildServiceProvider(this ServiceCollection collection)
        {
            return new ServiceProvider(collection.FallbackServices).Add(collection);
        }
    }
}
