
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace GenericHostSample
{
    public class ProgramNoDisposeHangs
    {
        public static async Task Main(string[] args)
        {
            // Hangs due to #1363
            var host = CreateHostBuilder(args).Build();
            await host.StartAsync();
            await host.StopAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => services.AddHostedService<MyServiceA>());
    }
}
