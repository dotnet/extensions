// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Latency.Test;

public class RequestCheckpointExtensionsTests
{
    [Fact]
    public void AddRequestCheckpoint_Throws_WhenNullBuilder()
    {
        Assert.Throws<ArgumentNullException>(() => Extensions.DependencyInjection.RequestLatencyTelemetryServiceCollectionExtensions.AddRequestCheckpoint(null!));
    }

    [Fact]
    public void UseRequestCheckpoint_Throws_WhenNullBuilder()
    {
        Assert.Throws<ArgumentNullException>(() => Builder.RequestLatencyTelemetryApplicationBuilderExtensions.UseRequestCheckpoint(null!));
    }

    [Fact]
    public void AddPipelineEntryCheckpoint_Throws_WhenNullBuilder()
    {
        Assert.Throws<ArgumentNullException>(() => Extensions.DependencyInjection.RequestLatencyTelemetryServiceCollectionExtensions.AddPipelineEntryCheckpoint(null!));
    }
}
