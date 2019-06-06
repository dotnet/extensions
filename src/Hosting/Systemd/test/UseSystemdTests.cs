// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public class UseSystemdTests
    {
        [Fact]
        public void DefaultsToOffOutsideOfService()
        {
            var host = new HostBuilder()
                .UseSystemd()
                .Build();

            using (host)
            {
                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsType<ConsoleLifetime>(lifetime);
            }
        }
    }
}
