// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Core.Tests;

public class ApplicationLifecycleHealthChecksExtensionsTest
{
    [Fact]
    public void AddApplicationLifecycleHealthCheck_DependenciesAreRegistered()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IHostApplicationLifetime>(new Mock<IHostApplicationLifetime>().Object);
        serviceCollection.AddHealthChecks().AddApplicationLifecycleHealthCheck();

        AssertAddedHealthCheck<ApplicationLifecycleHealthCheck>(serviceCollection);
    }

    [Fact]
    public void AddApplicationLifecycleHealthCheck_WithTags_DependenciesAreRegistered()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IHostApplicationLifetime>(new Mock<IHostApplicationLifetime>().Object);
        serviceCollection.AddHealthChecks().AddApplicationLifecycleHealthCheck(new[] { "test1", "test2" });

        AssertAddedHealthCheck<ApplicationLifecycleHealthCheck>(serviceCollection);
    }

    [Fact]
    public void TestNullChecks()
    {
        Assert.Throws<ArgumentNullException>(() => ((IHealthChecksBuilder)null!).AddApplicationLifecycleHealthCheck());
        Assert.Throws<ArgumentNullException>(() => ((IHealthChecksBuilder)null!).AddApplicationLifecycleHealthCheck(null!));
    }

    private static void AssertAddedHealthCheck<T>(IServiceCollection serviceCollection)
    {
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var registrations = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        Assert.Single(registrations);
        foreach (var r in registrations)
        {
            Assert.True(r.Factory(serviceProvider) is T);
            Assert.Equal("ApplicationLifecycleHealthCheck", r.Name);
        }
    }
}
