// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    private sealed class DerivedAITool : AITool;
}
