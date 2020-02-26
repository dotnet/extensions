// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GenericHostSample
{
    public class ProgramExternallyControlled
    {
        private IHost _host;

        public ProgramExternallyControlled()
        {
            _host = new HostBuilder()
                .UseServiceProviderFactory<MyContainer>(new MyContainerFactory())
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                })
                .ConfigureContainer<MyContainer>((hostContext, container) =>
                {
                })
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddEnvironmentVariables();
                    config.AddJsonFile("appsettings.json", optional: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<MyServiceA>();
                    services.AddHostedService<MyServiceB>();
                })
                .Build();
        }

        public void Start()
        {
            _host.Start();
        }

        public async Task StopAsync()
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
    }
}
