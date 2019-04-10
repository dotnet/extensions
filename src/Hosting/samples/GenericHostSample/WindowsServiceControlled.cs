using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GenericHostSample
{
    public class WindowsServiceControlled
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<MyServiceA>();
                    services.AddHostedService<MyServiceB>();
                });

            builder.UseWindowsService();
            await builder.Build().RunAsync();
        }
    }
}
