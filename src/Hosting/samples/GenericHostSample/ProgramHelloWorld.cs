using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace GenericHostSample
{
    public class ProgramHelloWorld
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<MyServiceA>();
                    services.AddHostedService<MyServiceB>();
                });

            await builder.RunConsoleAsync(options =>
            {
                options.SuppressStatusMessages = false;
            });
        }
    }
}
