﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

public sealed class ResourceMonitoringOptionsTests
{
    [Fact]
    public void Basic()
    {
        var options = new ResourceMonitoringOptions
        {
            CollectionWindow = TimeSpan.FromMilliseconds(100),
            SamplingInterval = TimeSpan.FromMilliseconds(10),
            PublishingWindow = TimeSpan.FromSeconds(50)
        };

        Assert.NotNull(options);
        Assert.False(options.CalculateCpuUsageWithoutHost);
    }

    [Fact]
    public void CalculateCpuUsageWithoutHost_WhenSet_ReturnsExpectedValue()
    {
        var options = new ResourceMonitoringOptions
        {
            CalculateCpuUsageWithoutHost = true
        };

        Assert.True(options.CalculateCpuUsageWithoutHost);
    }
}
