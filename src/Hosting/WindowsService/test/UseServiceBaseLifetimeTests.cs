// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Hosting.WindowsService;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public class UseServiceBaseLifetimeTests
    {
        [Fact]
        public void CanOptOutViaConfig()
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string>("service", "false")
                    });
                })
                .UseServiceBaseLifetime()
                .Build();

            using (host)
            {
                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsType<ConsoleLifetime>(lifetime);
            }
        }

        [Fact]
        public void CanOptInViaConfig()
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string>("service", "true")
                    });
                })
                .UseServiceBaseLifetime()
                .Build();

            using (host)
            {
                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsType<ServiceBaseLifetime>(lifetime);
            }
        }

        [Fact]
        public void CanOptOutViaDevelopment()
        {
            var host = new HostBuilder()
                .ConfigureHostConfiguration(config =>
                {
                    config.AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string>("environment", "development")
                    });
                })
                .UseServiceBaseLifetime()
                .Build();

            using (host)
            {
                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsType<ConsoleLifetime>(lifetime);
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, "Only available on Windows")]
        [OSSkipCondition(OperatingSystems.MacOSX, "Only available on Windows")]
        public void CanOptInViaProduction()
        {
            var host = new HostBuilder()
                .ConfigureHostConfiguration(config =>
                {
                    config.AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string>("environment", "production")
                    });
                })
                .UseServiceBaseLifetime()
                .Build();

            using (host)
            {
                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsType<ServiceBaseLifetime>(lifetime);
            }
        }
    }
}
