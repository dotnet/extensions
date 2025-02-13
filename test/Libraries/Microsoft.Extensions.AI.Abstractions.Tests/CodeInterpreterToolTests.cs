// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.AI;

public class CodeInterpreterToolTests
{
    [Fact]
    public void Constructor_Roundtrips()
    {
        var tool = new CodeInterpreterTool();
        Assert.Equal(nameof(CodeInterpreterTool), tool.Name);
        Assert.Empty(tool.Description);
        Assert.Empty(tool.AdditionalProperties);
        Assert.Equal(nameof(CodeInterpreterTool), tool.ToString());

        var props = new AdditionalPropertiesDictionary();
        tool = new CodeInterpreterTool(props);
        Assert.Equal(nameof(CodeInterpreterTool), tool.Name);
        Assert.Empty(tool.Description);
        Assert.Same(props, tool.AdditionalProperties);
        Assert.Equal(nameof(CodeInterpreterTool), tool.ToString());
    }
}
