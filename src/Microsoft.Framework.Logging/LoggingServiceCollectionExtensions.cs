using Microsoft.Framework.Logging;

namespace Microsoft.Framework.DependencyInjection
{
    public static class LoggingServiceCollectionExtensions
    {
        public static IServiceCollection AddLogging(this IServiceCollection services)
        {
            services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, LoggerFactory>());
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
            return services;
        }
    }
}