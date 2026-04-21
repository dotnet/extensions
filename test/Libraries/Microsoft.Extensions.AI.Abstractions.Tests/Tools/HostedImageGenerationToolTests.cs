// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedImageGenerationToolTests
{
    [Fact]
    public void Constructor_Roundtrips()
    {
        var tool = new HostedImageGenerationTool();
        Assert.Equal("image_generation", tool.Name);
        Assert.Empty(tool.Description);
        Assert.Empty(tool.AdditionalProperties);
        Assert.Null(tool.Options);
        Assert.Equal(tool.Name, tool.ToString());
    }

    [Fact]
    public void Constructor_AdditionalProperties_Roundtrips()
    {
        var props = new Dictionary<string, object?> { ["key"] = "value" };
        var tool = new HostedImageGenerationTool(props);

        Assert.Equal("image_generation", tool.Name);
        Assert.Same(props, tool.AdditionalProperties);
    }

    [Fact]
    public void Constructor_NullAdditionalProperties_UsesEmpty()
    {
        var tool = new HostedImageGenerationTool(null);

        Assert.Empty(tool.AdditionalProperties);
    }

    [Fact]
    public void Options_Roundtrip()
    {
        var options = new ImageGenerationOptions();
        var tool = new HostedImageGenerationTool
        {
            Options = options
        };

        Assert.Same(options, tool.Options);
    }
}
