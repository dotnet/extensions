// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public class UseSystemdTests
    {
        [Fact(Skip = "Product issue fixed by https://github.com/dotnet/extensions/pull/2734. This change won't be backported unless requested.")]
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
