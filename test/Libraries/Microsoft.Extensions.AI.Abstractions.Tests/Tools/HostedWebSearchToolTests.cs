// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedWebSearchToolTests
{
    [Fact]
    public void Constructor_Roundtrips()
    {
        var tool = new HostedWebSearchTool();
        Assert.Equal("web_search", tool.Name);
        Assert.Empty(tool.Description);
        Assert.Empty(tool.AdditionalProperties);
        Assert.Equal(tool.Name, tool.ToString());
    }

    [Fact]
    public void Constructor_AdditionalProperties_Roundtrips()
    {
        var props = new Dictionary<string, object?> { ["key"] = "value" };
        var tool = new HostedWebSearchTool(props);

        Assert.Equal("web_search", tool.Name);
        Assert.Same(props, tool.AdditionalProperties);
    }

    [Fact]
    public void Constructor_NullAdditionalProperties_UsesEmpty()
    {
        var tool = new HostedWebSearchTool(null);

        Assert.Empty(tool.AdditionalProperties);
    }
}
