using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Cache.Session;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Cache.Distributed;
using Microsoft.Framework.Cache.Memory;
using Microsoft.Framework.Cache.Redis;

namespace SessionSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseInMemorySession();
            // app.UseDistributedSession(new RedisCache(new RedisCacheOptions() { Configuration = "localhost" }));

            app.Run(async context =>
            {
                int visits = 0;
                visits = context.GetSession().GetInt("visits") ?? 0;
                context.GetSession().SetInt("visits", ++visits);
                await context.Response.WriteAsync("You have visited our page this many times: " + visits);
            });
        }
    }
}
