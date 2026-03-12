// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedToolSearchToolTests
{
    [Fact]
    public void Constructor_Roundtrips()
    {
        var tool = new HostedToolSearchTool();
        Assert.Equal("tool_search", tool.Name);
        Assert.Empty(tool.Description);
        Assert.Empty(tool.AdditionalProperties);
        Assert.Equal(tool.Name, tool.ToString());
    }

    [Fact]
    public void Constructor_AdditionalProperties_Roundtrips()
    {
        var props = new Dictionary<string, object?> { ["key"] = "value" };
        var tool = new HostedToolSearchTool(props);

        Assert.Equal("tool_search", tool.Name);
        Assert.Same(props, tool.AdditionalProperties);
    }

    [Fact]
    public void Constructor_NullAdditionalProperties_UsesEmpty()
    {
        var tool = new HostedToolSearchTool(null);

        Assert.Empty(tool.AdditionalProperties);
    }

    [Fact]
    public void DeferredTools_DefaultIsNull()
    {
        var tool = new HostedToolSearchTool();
        Assert.Null(tool.DeferredTools);
    }

    [Fact]
    public void DeferredTools_Roundtrips()
    {
        var tool = new HostedToolSearchTool();
        var list = new List<string> { "func1", "func2" };
        tool.DeferredTools = list;
        Assert.Same(list, tool.DeferredTools);
    }

    [Fact]
    public void NonDeferredTools_DefaultIsNull()
    {
        var tool = new HostedToolSearchTool();
        Assert.Null(tool.NonDeferredTools);
    }

    [Fact]
    public void NonDeferredTools_Roundtrips()
    {
        var tool = new HostedToolSearchTool();
        var list = new List<string> { "func3" };
        tool.NonDeferredTools = list;
        Assert.Same(list, tool.NonDeferredTools);
    }
}
