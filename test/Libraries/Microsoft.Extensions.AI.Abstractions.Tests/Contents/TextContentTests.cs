// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.AI;

public class TextContentTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("text")]
    public void Constructor_String_PropsDefault(string? text)
    {
        TextContent c = new(text);
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Equal(text, c.Text);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        TextContent c = new(null);

        Assert.Null(c.RawRepresentation);
        object raw = new();
        c.RawRepresentation = raw;
        Assert.Same(raw, c.RawRepresentation);

        Assert.Null(c.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        c.AdditionalProperties = props;
        Assert.Same(props, c.AdditionalProperties);

        Assert.Null(c.Text);
        c.Text = "text";
        Assert.Equal("text", c.Text);
        Assert.Equal("text", c.ToString());

        c.Text = null;
        Assert.Null(c.Text);
        Assert.Equal(string.Empty, c.ToString());
    }
}
