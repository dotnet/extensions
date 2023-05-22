// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Connections.Test;

public class ConnectionTimeoutExtensionsTests
{
    [Fact]
    public async Task AddConnectionTimeout_ShouldAddOptionsValidators_WhenUsingFunc()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddConnectionTimeout(c => c.Timeout = TimeSpan.FromDays(100)))
            .Build();

        var ex = await Record.ExceptionAsync(() => host.StartAsync()).ConfigureAwait(false);
        Assert.IsType<OptionsValidationException>(ex);
    }

    [Fact]
    public async Task AddConnectionTimeout_ShouldAddOptionsValidators_WhenUsingConfig()
    {
        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(new List<KeyValuePair<string, string?>>
                {
                    new("Timeout:Timeout", "01:00:01") // One hour and one second.
                })
                .Build()
                .GetSection("Timeout");

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
            .AddConnectionTimeout(configuration))
            .Build();

        var ex = await Record.ExceptionAsync(() => host.StartAsync()).ConfigureAwait(false);
        Assert.IsType<OptionsValidationException>(ex);
    }
}
