// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Test;

public class ManualHealthCheckExtensionsTests
{
    [Fact]
    public void AddManualHealthCheck_DependenciesAreRegistered()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHealthChecks().AddManualHealthCheck();

        AssertAddedHealthCheck<ManualHealthCheckService>(serviceCollection, "Manual");
    }

    [Fact]
    public void AddManualHealthCheck_WithTags_DependenciesAreRegistered()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHealthChecks().AddManualHealthCheck("test1", "test2");

        AssertAddedHealthCheck<ManualHealthCheckService>(serviceCollection, "Manual");
    }

    [Fact]
    public void AddManualHealthCheck_WithTagsEnumerable_DependenciesAreRegistered()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHealthChecks().AddManualHealthCheck(new List<string> { "test1", "test2" });

        AssertAddedHealthCheck<ManualHealthCheckService>(serviceCollection, "Manual");
    }

    [Fact]
    public void TestNullChecks()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddHealthChecks().AddManualHealthCheck());
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddHealthChecks().AddManualHealthCheck(null!));
    }

    private static void AssertAddedHealthCheck<T>(IServiceCollection serviceCollection, string name)
    {
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var registrations = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        Assert.Single(registrations);
        foreach (var r in registrations)
        {
            _ = Assert.IsType<T>(r.Factory(serviceProvider));
            Assert.Equal(r.Name, name);
        }
    }
}
