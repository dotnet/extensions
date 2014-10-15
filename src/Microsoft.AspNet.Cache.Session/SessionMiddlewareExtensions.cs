using System;
using Microsoft.AspNet.Cache.Session;
using Microsoft.Framework.Cache.Distributed;
using Microsoft.Framework.Cache.Memory;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Builder
{
    public static class SessionMiddlewareExtensions
    {
        public static IServiceCollection ConfigureSession([NotNull] this IServiceCollection services, [NotNull] Action<SessionOptions> configure)
        {
            return services.ConfigureOptions(configure);
        }

        public static IApplicationBuilder UseInMemorySession([NotNull] this IApplicationBuilder app, Action<SessionOptions> configure = null)
        {
            return app.UseMiddleware<SessionMiddleware>(
                new ConfigureOptions<SessionOptions>(options =>
                {
                    options.Store = new DistributedSessionStore(new LocalCache(new MemoryCacheOptions()));
                    if (configure != null)
                    {
                        configure(options);
                    }
                }));
        }

        public static IApplicationBuilder UseDistributedSession([NotNull] this IApplicationBuilder app, IDistributedCache cache, Action<SessionOptions> configure = null)
        {
            return app.UseMiddleware<SessionMiddleware>(
                new ConfigureOptions<SessionOptions>(options =>
                {
                    options.Store = new DistributedSessionStore(cache);
                    if (configure != null)
                    {
                        configure(options);
                    }
                }));
        }

        public static IApplicationBuilder UseSession([NotNull] this IApplicationBuilder app, Action<SessionOptions> configure = null)
        {
            return app.UseMiddleware<SessionMiddleware>(
                new ConfigureOptions<SessionOptions>(configure ?? (o => { }))
                {
                    Name = string.Empty
                });
        }
    }
}