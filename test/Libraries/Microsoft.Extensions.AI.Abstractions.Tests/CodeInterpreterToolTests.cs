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
        Assert.Null(tool.AdditionalProperties);

        var props = new AdditionalPropertiesDictionary();
        tool.AdditionalProperties = props;
        Assert.Same(props, tool.AdditionalProperties);
    }
}
