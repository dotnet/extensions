// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.AI;

public class AIContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        DerivedAIContent c = new();
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        DerivedAIContent c = new();

        Assert.Null(c.RawRepresentation);
        object raw = new();
        c.RawRepresentation = raw;
        Assert.Same(raw, c.RawRepresentation);

        Assert.Null(c.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        c.AdditionalProperties = props;
        Assert.Same(props, c.AdditionalProperties);
    }

    private sealed class DerivedAIContent : AIContent;
}
