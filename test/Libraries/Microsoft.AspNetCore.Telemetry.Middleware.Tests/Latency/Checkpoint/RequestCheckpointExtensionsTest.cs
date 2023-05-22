// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Telemetry;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Test;

public class RequestCheckpointExtensionsTest
{
    [Fact]
    public void AddRequestCheckpoint_Throws_WhenNullBuilder()
    {
        Assert.Throws<ArgumentNullException>(() => RequestCheckpointExtensions.AddRequestCheckpoint(null!));
    }

    [Fact]
    public void UseRequestCheckpoint_Throws_WhenNullBuilder()
    {
        Assert.Throws<ArgumentNullException>(() => RequestCheckpointExtensions.UseRequestCheckpoint(null!));
    }

    [Fact]
    public void AddPipelineEntryCheckpoint_Throws_WhenNullBuilder()
    {
        Assert.Throws<ArgumentNullException>(() => RequestCheckpointExtensions.AddPipelineEntryCheckpoint(null!));
    }
}
