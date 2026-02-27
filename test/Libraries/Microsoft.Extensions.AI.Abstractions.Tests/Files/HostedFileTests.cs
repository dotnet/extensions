// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedFileTests
{
    [Fact]
    public void Constructor_NullId_Throws()
    {
        Assert.Throws<ArgumentNullException>("id", () => new HostedFile(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Constructor_EmptyOrWhitespaceId_Throws(string id)
    {
        Assert.Throws<ArgumentException>(nameof(id), () => new HostedFile(id));
    }

    [Fact]
    public void Constructor_PropsDefault()
    {
        var file = new HostedFile("file-123");
        Assert.Equal("file-123", file.Id);
        Assert.Null(file.Name);
        Assert.Null(file.MediaType);
        Assert.Null(file.SizeInBytes);
        Assert.Null(file.CreatedAt);
        Assert.Null(file.Purpose);
        Assert.Null(file.AdditionalProperties);
        Assert.Null(file.RawRepresentation);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        var file = new HostedFile("file-123");
        var now = DateTimeOffset.UtcNow;
        var props = new AdditionalPropertiesDictionary { { "key", "value" } };
        var raw = new object();

        file.Name = "test.txt";
        file.MediaType = "text/plain";
        file.SizeInBytes = 12345L;
        file.CreatedAt = now;
        file.Purpose = "assistants";
        file.AdditionalProperties = props;
        file.RawRepresentation = raw;

        Assert.Equal("test.txt", file.Name);
        Assert.Equal("text/plain", file.MediaType);
        Assert.Equal(12345L, file.SizeInBytes);
        Assert.Equal(now, file.CreatedAt);
        Assert.Equal("assistants", file.Purpose);
        Assert.Same(props, file.AdditionalProperties);
        Assert.Same(raw, file.RawRepresentation);
    }

    [Fact]
    public void ToHostedFileContent_CreatesCorrectContent()
    {
        var file = new HostedFile("file-123")
        {
            Name = "test.txt",
            MediaType = "text/plain"
        };

        HostedFileContent content = file.ToHostedFileContent();

        Assert.Equal("file-123", content.FileId);
        Assert.Equal("test.txt", content.Name);
        Assert.Equal("text/plain", content.MediaType);
    }

    [Fact]
    public void ToHostedFileContent_NullOptionalProperties()
    {
        var file = new HostedFile("file-456");

        HostedFileContent content = file.ToHostedFileContent();

        Assert.Equal("file-456", content.FileId);
        Assert.Null(content.Name);
        Assert.Null(content.MediaType);
    }
}
