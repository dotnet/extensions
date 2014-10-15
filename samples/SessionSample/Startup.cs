using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Cache.Session;
using Microsoft.AspNet.Http;

namespace SessionSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseInMemorySession();
            // app.UseDistributedSession(new RedisCache(new RedisCacheOptions() { Configuration = "localhost" }));

            app.Map("/session", subApp =>
            {
                subApp.Run(async context =>
                {
                    int visits = 0;
                    visits = context.GetSession().GetInt("visits") ?? 0;
                    context.GetSession().SetInt("visits", ++visits);
                    await context.Response.WriteAsync("Counting: You have visited our page this many times: " + visits);
                });
            });

            app.Run(async context =>
            {
                int visits = 0;
                visits = context.GetSession().GetInt("visits") ?? 0;
                // context.GetSession().SetInt("visits", ++visits);
                await context.Response.WriteAsync("Home: You have visited our page this many times: " + visits);
            });
        }
    }
}
