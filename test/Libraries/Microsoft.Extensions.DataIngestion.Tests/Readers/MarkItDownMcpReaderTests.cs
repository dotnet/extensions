// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Readers.Tests;

public class MarkItDownMcpReaderTests
{
    [Fact]
    public void Constructor_ThrowsWhenMcpServerUriIsNull()
    {
        Assert.Throws<ArgumentNullException>("mcpServerUri", () => new MarkItDownMcpReader(null!));
    }

    [Fact]
    public async Task ReadAsync_ThrowsWhenIdentifierIsNull()
    {
        var reader = new MarkItDownMcpReader(new Uri("http://localhost:3001/sse"));

        await Assert.ThrowsAsync<ArgumentNullException>("identifier", async () => await reader.ReadAsync(new FileInfo("fileName.txt"), identifier: null!));
        await Assert.ThrowsAsync<ArgumentException>("identifier", async () => await reader.ReadAsync(new FileInfo("fileName.txt"), identifier: string.Empty));

        using MemoryStream stream = new();
        await Assert.ThrowsAsync<ArgumentNullException>("identifier", async () => await reader.ReadAsync(stream, identifier: null!, mediaType: "some"));
        await Assert.ThrowsAsync<ArgumentException>("identifier", async () => await reader.ReadAsync(stream, identifier: string.Empty, mediaType: "some"));
    }

    [Fact]
    public async Task ReadAsync_ThrowsWhenSourceIsNull()
    {
        var reader = new MarkItDownMcpReader(new Uri("http://localhost:3001/sse"));

        await Assert.ThrowsAsync<ArgumentNullException>("source", async () => await reader.ReadAsync(null!, "identifier"));
        await Assert.ThrowsAsync<ArgumentNullException>("source", async () => await reader.ReadAsync((Stream)null!, "identifier", "mediaType"));
    }

    [Fact]
    public async Task ReadAsync_ThrowsWhenFileDoesNotExist()
    {
        var reader = new MarkItDownMcpReader(new Uri("http://localhost:3001/sse"));
        var nonExistentFile = new FileInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

        await Assert.ThrowsAsync<FileNotFoundException>(async () => await reader.ReadAsync(nonExistentFile, "identifier"));
    }

    // NOTE: Integration tests with an actual MCP server would go here, but they would require
    // a running MarkItDown MCP server to be available, which is not part of the test setup.
    // For full integration testing, use a real MCP server in a separate test environment.
}
