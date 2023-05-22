// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.TestUtilities;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Test;

[Collection("Tcp Connection Tests")]
public sealed class WindowsUtilizationExtensionsTest
{
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Windows specific.")]
    public void AddWindowsProvider_Adds_WindowsResourceUtilizationProvider_To_ServiceCollection()
    {
        var builderMock = new Mock<IResourceUtilizationTrackerBuilder>();
        var services = new ServiceCollection()
            .AddLogging();
        builderMock.Setup(builder => builder.Services).Returns(services);

        builderMock.Object
            .AddWindowsProvider()
            .AddWindowsPerfCounterPublisher();

        var descriptor = services.Single(d => d.ServiceType == typeof(ISnapshotProvider));
        Assert.NotNull(descriptor);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Windows specific.")]
    public void AddWindowsPerfCounterPublisher_Adds_WindowsPerfCounterPublisher_To_ServiceCollection()
    {
        var builderMock = new Mock<IResourceUtilizationTrackerBuilder>(MockBehavior.Loose);
        var services = new ServiceCollection()
            .AddLogging();
        builderMock.Setup(builder => builder.Services).Returns(services);

        builderMock.Object
            .AddWindowsProvider()
            .AddWindowsPerfCounterPublisher();

        builderMock.Verify(b => b.AddPublisher<WindowsPerfCounterPublisher>());
    }

    [Fact]
    public void AddWindowsCounters_Adds_WindowsCounters_To_ServiceCollection()
    {
        var builderMock = new Mock<IResourceUtilizationTrackerBuilder>(MockBehavior.Loose);
        var services = new ServiceCollection()
            .AddLogging();
        builderMock.Setup(builder => builder.Services).Returns(services);

        builderMock.Object
            .AddWindowsCounters();

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<WindowsCounters>());
    }

    [Fact]
    public void AddWindowsCounters_Adds_WindowsCounters_To_ServiceCollection_With_ConfigurationSection()
    {
        var builderMock = new Mock<IResourceUtilizationTrackerBuilder>(MockBehavior.Loose);
        var configurationMock = new Mock<IConfigurationSection>(MockBehavior.Loose);
        var services = new ServiceCollection()
            .AddLogging();
        builderMock.Setup(builder => builder.Services).Returns(services);

        builderMock.Object
            .AddWindowsCounters(configurationMock.Object);

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<WindowsCounters>());
    }

    [Fact]
    public void AddWindowsCounters_Adds_WindowsCounters_To_ServiceCollection_With_Action()
    {
        var builderMock = new Mock<IResourceUtilizationTrackerBuilder>(MockBehavior.Loose);
        var actionMock = new Mock<Action<WindowsCountersOptions>>(MockBehavior.Loose);
        var services = new ServiceCollection()
            .AddLogging();
        builderMock.Setup(builder => builder.Services).Returns(services);

        builderMock.Object
            .AddWindowsCounters(actionMock.Object);

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<WindowsCounters>());
    }

    [Fact]
    public void VerifyHostBuilderNullCheck()
    {
        Assert.Throws<ArgumentNullException>(() => WindowsUtilizationExtensions.AddWindowsProvider(null!));
        Assert.Throws<ArgumentNullException>(() => WindowsUtilizationExtensions.AddWindowsPerfCounterPublisher(null!));
    }
}
