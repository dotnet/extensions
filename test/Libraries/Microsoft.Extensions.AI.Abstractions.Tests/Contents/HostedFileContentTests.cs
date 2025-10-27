// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedFileContentTests
{
    [Fact]
    public void Constructor_InvalidInput_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new HostedFileContent(null!));
        Assert.Throws<ArgumentException>(() => new HostedFileContent(string.Empty));
        Assert.Throws<ArgumentException>(() => new HostedFileContent(" "));
    }

    [Fact]
    public void Constructor_String_PropsDefault()
    {
        string fileId = "id123";
        HostedFileContent c = new(fileId);
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Equal(fileId, c.FileId);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        HostedFileContent c = new("id123");
        Assert.Equal("id123", c.FileId);

        c.FileId = "id456";
        Assert.Equal("id456", c.FileId);

        Assert.Throws<ArgumentNullException>(() => c.FileId = null!);
        Assert.Throws<ArgumentException>(() => c.FileId = string.Empty);
        Assert.Throws<ArgumentException>(() => c.FileId = " ");

        Assert.Null(c.RawRepresentation);
        object raw = new();
        c.RawRepresentation = raw;
        Assert.Same(raw, c.RawRepresentation);

        Assert.Null(c.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        c.AdditionalProperties = props;
        Assert.Same(props, c.AdditionalProperties);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        var content = new HostedFileContent("file123");

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedContent = JsonSerializer.Deserialize<HostedFileContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedContent);
        Assert.Equal(content.FileId, deserializedContent.FileId);
    }
}
