// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Diagnostics.Logging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Latency.Test;

public class HttpLatencyTelemetryExtensionsTests
{
    [Fact]
    public void AddHttpLatencyTelemetry_NullArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            HttpLatencyTelemetryExtensions.AddHttpLatencyTelemetry(null!));
    }

    [Fact]
    public void AddHttpLatencyTelemetry_RegistersEnricher()
    {
        using var serviceProvider = new ServiceCollection()
            .AddHttpLatencyTelemetry()
            .BuildServiceProvider();

        var enricher = serviceProvider.GetRequiredService<IHttpLogEnricher>();

        Assert.NotNull(enricher);
        Assert.IsType<HttpLatencyLogEnricher>(enricher);
    }
}
