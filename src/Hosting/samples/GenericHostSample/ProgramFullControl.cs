// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GenericHostSample
{
    public class ProgramFullControl
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .UseServiceProviderFactory<MyContainer>(new MyContainerFactory())
                .ConfigureContainer<MyContainer>((hostContext, container) =>
                {
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                })
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddEnvironmentVariables();
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<MyServiceA>();
                    services.AddHostedService<MyServiceB>();
                })
                .Build();

            var s = host.Services;

            using (host)
            {
                Console.WriteLine("Starting!");

                await host.StartAsync();

                Console.WriteLine("Started! Press <enter> to stop.");

                Console.ReadLine();

                Console.WriteLine("Stopping!");

                await host.StopAsync();

                Console.WriteLine("Stopped!");
            }
        }
    }
}
