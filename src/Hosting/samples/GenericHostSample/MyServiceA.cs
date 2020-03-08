// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GenericHostSample
{
    public class MyServiceA : BackgroundService
    {
        public MyServiceA(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<MyServiceA>();
        }

        public ILogger Logger { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("MyServiceA is starting.");

            stoppingToken.Register(() => Logger.LogInformation("MyServiceA is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                Logger.LogInformation("MyServiceA is doing background work.");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            Logger.LogInformation("MyServiceA background task is stopping.");
        }
    }
}
