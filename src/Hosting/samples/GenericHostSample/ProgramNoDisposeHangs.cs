// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
