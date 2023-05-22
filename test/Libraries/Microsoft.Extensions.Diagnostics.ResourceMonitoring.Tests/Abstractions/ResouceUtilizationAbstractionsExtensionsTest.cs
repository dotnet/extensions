// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;
public sealed class ResouceUtilizationAbstractionsExtensionsTest
{
    [Fact]
    public void AddNullResourceUtilizationProvider_AddsNullSnapshotProvider_ToServicesCollection()
    {
        var services = new ServiceCollection();

        var builder = new Mock<IResourceUtilizationTrackerBuilder>(MockBehavior.Loose);
        builder.Setup(builder => builder.Services).Returns(services);

        using var servicesProvider = builder.Object.AddNullResourceUtilizationProvider()
            .Services.BuildServiceProvider();

        var snapshotProvider = servicesProvider.GetRequiredService<ISnapshotProvider>();

        Assert.NotNull(snapshotProvider);
        Assert.IsType<NullSnapshotProvider>(snapshotProvider);
        Assert.IsAssignableFrom<ISnapshotProvider>(snapshotProvider);
    }

    [Fact]
    public void AddNullResourceUtilization_AddsNullResourceUtilizationTrackerService_ToServicesCollection()
    {
        var services = new ServiceCollection();

        using var servicesProvider = services.AddNullResourceUtilization().BuildServiceProvider();

        var tracker = servicesProvider.GetRequiredService<IResourceUtilizationTracker>();

        Assert.NotNull(tracker);
        Assert.IsType<NullResourceUtilizationTrackerService>(tracker);
        Assert.IsAssignableFrom<IResourceUtilizationTracker>(tracker);
    }
}
