// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.AI;

public class BackgroundResponsesOptionsTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        BackgroundResponsesOptions options = new();
        Assert.Null(options.Allow);
    }

    [Fact]
    public void Constructor_Copy_PropsRoundtrip()
    {
        BackgroundResponsesOptions original = new()
        {
            Allow = true
        };

        BackgroundResponsesOptions copy = new(original);

        Assert.True(copy.Allow);
        Assert.NotSame(original, copy);
    }
}
