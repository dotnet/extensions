// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AIToolTests
{
    [Fact]
    public void Constructor_Roundtrips()
    {
        DerivedAITool tool = new();
        Assert.Equal(nameof(DerivedAITool), tool.Name);
        Assert.Equal(nameof(DerivedAITool), tool.ToString());
        Assert.Empty(tool.Description);
        Assert.Empty(tool.AdditionalProperties);
    }

    [Fact]
    public void GetService_ReturnsExpectedObject()
    {
        DerivedAITool tool = new();

        Assert.Throws<ArgumentNullException>("serviceType", () => tool.GetService(null!));

        Assert.Same(tool, tool.GetService(typeof(object)));
        Assert.Same(tool, tool.GetService(typeof(AITool)));
        Assert.Same(tool, tool.GetService(typeof(DerivedAITool)));

        Assert.Same(tool, tool.GetService<object>());
        Assert.Same(tool, tool.GetService<AITool>());
        Assert.Same(tool, tool.GetService<DerivedAITool>());

        Assert.Null(tool.GetService<object>("key"));
        Assert.Null(tool.GetService<AITool>("key"));
        Assert.Null(tool.GetService<DerivedAITool>("key"));
    }

    private sealed class DerivedAITool : AITool;
}
