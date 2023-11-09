// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Latency.Test;

public class NoopLatencyContextTests
{
    [Fact]
    public void ServiceCollection_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NullLatencyContextServiceCollectionExtensions.AddNullLatencyContext(null!));
    }

    [Fact]
    public void ServiceCollection_BasicAddNoopLatencyContext()
    {
        using var serviceProvider = new ServiceCollection()
            .AddNullLatencyContext()
            .BuildServiceProvider();

        var latencyContextProvider = serviceProvider.GetRequiredService<ILatencyContextProvider>();
        Assert.NotNull(latencyContextProvider);
        Assert.IsAssignableFrom<NullLatencyContext>(latencyContextProvider);
    }

    [Fact]
    public void ServiceCollection_GivenScopes_ReturnsDifferentInstanceForEachScope()
    {
        using var serviceProvider = new ServiceCollection()
            .AddNullLatencyContext()
            .BuildServiceProvider();

        var scope1 = serviceProvider.CreateScope();
        var scope2 = serviceProvider.CreateScope();

        // Get same instance within single scope.
        Assert.Equal(scope1.ServiceProvider.GetRequiredService<ILatencyContextProvider>(),
            scope1.ServiceProvider.GetRequiredService<ILatencyContextProvider>());
        Assert.Equal(scope1.ServiceProvider.GetRequiredService<ILatencyContextTokenIssuer>(),
            scope1.ServiceProvider.GetRequiredService<ILatencyContextTokenIssuer>());

        // Get same instance between different scopes
        Assert.Equal(scope1.ServiceProvider.GetRequiredService<ILatencyContextProvider>(),
            scope2.ServiceProvider.GetRequiredService<ILatencyContextProvider>());
        Assert.Equal(scope1.ServiceProvider.GetRequiredService<ILatencyContextTokenIssuer>(),
            scope2.ServiceProvider.GetRequiredService<ILatencyContextTokenIssuer>());

        scope1.Dispose();
        scope2.Dispose();
    }

    [Fact]
    public void NoopLatencyContext_BasicFunctionality()
    {
        using var np = new NullLatencyContext();

        ILatencyContextProvider lcp = np;
        Assert.NotNull(lcp.CreateContext());

        ILatencyContext context = np;
        ILatencyContextTokenIssuer issuer = np;
        context.SetTag(issuer.GetTagToken(string.Empty), string.Empty);
        context.AddCheckpoint(issuer.GetCheckpointToken(string.Empty));
        context.AddMeasure(issuer.GetMeasureToken(string.Empty), 0);
        context.RecordMeasure(issuer.GetMeasureToken(string.Empty), 0);
        var latencyData = context.LatencyData;
        Assert.Equal(0, latencyData.DurationTimestamp);
        Assert.True(latencyData.Checkpoints.Length == 0);
        Assert.True(latencyData.Measures.Length == 0);
        Assert.True(latencyData.Tags.Length == 0);
        context.Freeze();
    }
}
