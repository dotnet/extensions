// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedCodeInterpreterToolTests
{
    [Fact]
    public void Constructor_Roundtrips()
    {
        var tool = new HostedCodeInterpreterTool();
        Assert.Equal(nameof(HostedCodeInterpreterTool), tool.Name);
        Assert.Empty(tool.Description);
        Assert.Empty(tool.AdditionalProperties);
        Assert.Equal(nameof(HostedCodeInterpreterTool), tool.ToString());
    }
}
