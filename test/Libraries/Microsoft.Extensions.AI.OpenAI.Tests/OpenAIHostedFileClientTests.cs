// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Containers;
using OpenAI.Files;
using Xunit;

#pragma warning disable S103 // Lines should not be too long
#pragma warning disable MEAI001

namespace Microsoft.Extensions.AI;

public class OpenAIHostedFileClientTests
{
    [Fact]
    public void AsIHostedFileClient_OpenAIClient_NullThrows()
    {
        Assert.Throws<ArgumentNullException>("openAIClient", () => ((OpenAIClient)null!).AsIHostedFileClient());
    }

    [Fact]
    public void AsIHostedFileClient_OpenAIFileClient_NullThrows()
    {
        Assert.Throws<ArgumentNullException>("fileClient", () => ((OpenAIFileClient)null!).AsIHostedFileClient());
    }

    [Fact]
    public void AsIHostedFileClient_ContainerClient_NullThrows()
    {
        Assert.Throws<ArgumentNullException>("containerClient", () => ((ContainerClient)null!).AsIHostedFileClient());
    }

    [Fact]
    public void AsIHostedFileClient_OpenAIClient_ProducesExpectedMetadata()
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        var client = new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        using IHostedFileClient fileClient = client.AsIHostedFileClient();
        var metadata = fileClient.GetService<HostedFileClientMetadata>();

        Assert.NotNull(metadata);
        Assert.Equal("openai", metadata.ProviderName);
        Assert.Equal(endpoint, metadata.ProviderUri);
    }

    [Fact]
    public void AsIHostedFileClient_OpenAIFileClient_ProducesExpectedMetadata()
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        var client = new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        using IHostedFileClient fileClient = client.GetOpenAIFileClient().AsIHostedFileClient();
        var metadata = fileClient.GetService<HostedFileClientMetadata>();

        Assert.NotNull(metadata);
        Assert.Equal("openai", metadata.ProviderName);
        Assert.Equal(endpoint, metadata.ProviderUri);
    }

    [Fact]
    public void AsIHostedFileClient_ContainerClient_ProducesExpectedMetadata()
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        var client = new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        using IHostedFileClient fileClient = client.GetContainerClient().AsIHostedFileClient();
        var metadata = fileClient.GetService<HostedFileClientMetadata>();

        Assert.NotNull(metadata);
        Assert.Equal("openai", metadata.ProviderName);
        Assert.Equal(endpoint, metadata.ProviderUri);
    }

    [Fact]
    public async Task Upload_DefaultPurpose_SendsAssistants()
    {
        const string ExpectedInput = """
            {
                "purpose": "assistants"
            }
            """;

        const string Output = """
            {
                "id": "file-abc123",
                "object": "file",
                "bytes": 140,
                "created_at": 1613677385,
                "filename": "mydata.jsonl",
                "purpose": "assistants"
            }
            """;

        using VerbatimMultiPartHttpHandler handler = new(ExpectedInput, Output) { ExpectedRequestUriContains = "files" };
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        using var stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });
        var result = await client.UploadAsync(stream);

        Assert.NotNull(result);
        Assert.Equal("file-abc123", result.Id);
        Assert.Equal("mydata.jsonl", result.Name);
        Assert.Equal("Assistants", result.Purpose);
        Assert.Equal(140, result.SizeInBytes);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_613_677_385), result.CreatedAt);
        Assert.Equal("application/x-ndjson", result.MediaType);
        Assert.NotNull(result.RawRepresentation);
    }

    [Fact]
    public async Task Upload_ExplicitPurposeFineTune_SendsFineTune()
    {
        const string ExpectedInput = """
            {
                "purpose": "fine-tune"
            }
            """;

        const string Output = """
            {
                "id": "file-def456",
                "object": "file",
                "bytes": 200,
                "created_at": 1613677400,
                "filename": "training.jsonl",
                "purpose": "fine-tune"
            }
            """;

        using VerbatimMultiPartHttpHandler handler = new(ExpectedInput, Output) { ExpectedRequestUriContains = "files" };
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        using var stream = new MemoryStream(new byte[] { 0x01, 0x02 });
        var result = await client.UploadAsync(stream, options: new HostedFileUploadOptions { Purpose = "fine-tune" });

        Assert.NotNull(result);
        Assert.Equal("file-def456", result.Id);
        Assert.Equal("FineTune", result.Purpose);
    }

    [Fact]
    public async Task Upload_NullContent_ThrowsArgumentNullException()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateFileClient(httpClient);

        await Assert.ThrowsAsync<ArgumentNullException>("content", () => client.UploadAsync(null!));
    }

    [Fact]
    public async Task Download_NullFileId_Throws()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateFileClient(httpClient);

        await Assert.ThrowsAsync<ArgumentNullException>("fileId", () => client.DownloadAsync(null!));
    }

    [Fact]
    public async Task Download_WhitespaceFileId_Throws()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateFileClient(httpClient);

        await Assert.ThrowsAsync<ArgumentException>("fileId", () => client.DownloadAsync("   "));
    }

    [Fact]
    public async Task GetFileInfo_ReturnsCorrectHostedFile()
    {
        const string Output = """
            {
                "id": "file-abc123",
                "object": "file",
                "bytes": 140,
                "created_at": 1613677385,
                "filename": "mydata.jsonl",
                "purpose": "assistants"
            }
            """;

        using VerbatimHttpHandler handler = new(new HttpHandlerExpectedInput { Body = null }, Output);
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        var result = await client.GetFileInfoAsync("file-abc123");

        Assert.NotNull(result);
        Assert.Equal("file-abc123", result.Id);
        Assert.Equal("mydata.jsonl", result.Name);
        Assert.Equal(140, result.SizeInBytes);
        Assert.Equal("Assistants", result.Purpose);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_613_677_385), result.CreatedAt);
        Assert.Equal("application/x-ndjson", result.MediaType);
        Assert.NotNull(result.RawRepresentation);
    }

    [Fact]
    public async Task GetFileInfo_NotFound_ReturnsNull()
    {
        using NotFoundHandler handler = new();
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        var result = await client.GetFileInfoAsync("file-nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFileInfo_NullFileId_Throws()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateFileClient(httpClient);

        await Assert.ThrowsAsync<ArgumentNullException>("fileId", () => client.GetFileInfoAsync(null!));
    }

    [Fact]
    public async Task GetFileInfo_WhitespaceFileId_Throws()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateFileClient(httpClient);

        await Assert.ThrowsAsync<ArgumentException>("fileId", () => client.GetFileInfoAsync("   "));
    }

    [Fact]
    public async Task ListFiles_ReturnsAllFiles()
    {
        const string Output = """
            {
                "data": [
                    {"id": "file-abc123", "object": "file", "bytes": 140, "created_at": 1613677385, "filename": "a.jsonl", "purpose": "assistants"},
                    {"id": "file-def456", "object": "file", "bytes": 200, "created_at": 1613677400, "filename": "b.jsonl", "purpose": "fine-tune"}
                ],
                "object": "list"
            }
            """;

        using VerbatimHttpHandler handler = new(new HttpHandlerExpectedInput { Body = null }, Output);
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        var files = await CollectAsync(client.ListFilesAsync());

        Assert.Equal(2, files.Count);

        Assert.Equal("file-abc123", files[0].Id);
        Assert.Equal("a.jsonl", files[0].Name);
        Assert.Equal(140, files[0].SizeInBytes);
        Assert.Equal("Assistants", files[0].Purpose);
        Assert.Equal("application/x-ndjson", files[0].MediaType);
        Assert.NotNull(files[0].RawRepresentation);

        Assert.Equal("file-def456", files[1].Id);
        Assert.Equal("b.jsonl", files[1].Name);
        Assert.Equal(200, files[1].SizeInBytes);
        Assert.Equal("FineTune", files[1].Purpose);
        Assert.Equal("application/x-ndjson", files[1].MediaType);
        Assert.NotNull(files[1].RawRepresentation);
    }

    [Fact]
    public async Task ListFiles_WithLimit_ReturnsLimitedCount()
    {
        const string Output = """
            {
                "data": [
                    {"id": "file-abc123", "object": "file", "bytes": 140, "created_at": 1613677385, "filename": "a.jsonl", "purpose": "assistants"},
                    {"id": "file-def456", "object": "file", "bytes": 200, "created_at": 1613677400, "filename": "b.jsonl", "purpose": "fine-tune"},
                    {"id": "file-ghi789", "object": "file", "bytes": 300, "created_at": 1613677500, "filename": "c.jsonl", "purpose": "assistants"}
                ],
                "object": "list"
            }
            """;

        using VerbatimHttpHandler handler = new(new HttpHandlerExpectedInput { Body = null }, Output);
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        var files = await CollectAsync(client.ListFilesAsync(new HostedFileListOptions { Limit = 2 }));

        Assert.Equal(2, files.Count);
    }

    [Fact]
    public async Task ListFiles_WithPurposeFilter()
    {
        const string Output = """
            {
                "data": [
                    {"id": "file-abc123", "object": "file", "bytes": 140, "created_at": 1613677385, "filename": "a.jsonl", "purpose": "assistants"}
                ],
                "object": "list"
            }
            """;

        using VerbatimHttpHandler handler = new(new HttpHandlerExpectedInput { Body = null }, Output);
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        var files = await CollectAsync(client.ListFilesAsync(new HostedFileListOptions { Purpose = "assistants" }));

        Assert.Single(files);
        Assert.Equal("file-abc123", files[0].Id);
        Assert.Equal("Assistants", files[0].Purpose);
    }

    [Fact]
    public async Task Delete_Success_ReturnsTrue()
    {
        const string Output = """
            {
                "id": "file-abc123",
                "object": "file",
                "deleted": true
            }
            """;

        using VerbatimHttpHandler handler = new(new HttpHandlerExpectedInput { Body = null }, Output);
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        var result = await client.DeleteAsync("file-abc123");

        Assert.True(result);
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsFalse()
    {
        using NotFoundHandler handler = new();
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        var result = await client.DeleteAsync("file-nonexistent");

        Assert.False(result);
    }

    [Fact]
    public async Task Delete_NullFileId_Throws()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateFileClient(httpClient);

        await Assert.ThrowsAsync<ArgumentNullException>("fileId", () => client.DeleteAsync(null!));
    }

    [Fact]
    public async Task Delete_WhitespaceFileId_Throws()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateFileClient(httpClient);

        await Assert.ThrowsAsync<ArgumentException>("fileId", () => client.DeleteAsync("   "));
    }

    [Fact]
    public async Task Upload_WithScope_OnFileOnlyClient_ThrowsInvalidOperation()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateFileOnlyClient(httpClient);

        using var stream = new MemoryStream(new byte[] { 0x01 });
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.UploadAsync(stream, options: new HostedFileUploadOptions { Scope = "container-123" }));
    }

    [Fact]
    public async Task Download_WithScope_OnFileOnlyClient_ThrowsInvalidOperation()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateFileOnlyClient(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DownloadAsync("file-abc123", new HostedFileDownloadOptions { Scope = "container-123" }));
    }

    [Fact]
    public async Task GetFileInfo_WithScope_OnFileOnlyClient_ThrowsInvalidOperation()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateFileOnlyClient(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetFileInfoAsync("file-abc123", new HostedFileGetOptions { Scope = "container-123" }));
    }

    [Fact]
    public async Task ListFiles_WithScope_OnFileOnlyClient_ThrowsInvalidOperation()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateFileOnlyClient(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await CollectAsync(client.ListFilesAsync(new HostedFileListOptions { Scope = "container-123" })));
    }

    [Fact]
    public async Task Delete_WithScope_OnFileOnlyClient_ThrowsInvalidOperation()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateFileOnlyClient(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DeleteAsync("file-abc123", new HostedFileDeleteOptions { Scope = "container-123" }));
    }

    [Fact]
    public async Task Upload_WithoutScope_OnContainerOnlyClient_ThrowsInvalidOperation()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateContainerClient(httpClient);

        using var stream = new MemoryStream(new byte[] { 0x01 });
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.UploadAsync(stream));
    }

    [Fact]
    public async Task Download_WithoutScope_OnContainerOnlyClient_ThrowsInvalidOperation()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateContainerClient(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DownloadAsync("file-abc123"));
    }

    [Fact]
    public async Task GetFileInfo_WithoutScope_OnContainerOnlyClient_ThrowsInvalidOperation()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateContainerClient(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetFileInfoAsync("file-abc123"));
    }

    [Fact]
    public async Task ListFiles_WithoutScope_OnContainerOnlyClient_ThrowsInvalidOperation()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateContainerClient(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await CollectAsync(client.ListFilesAsync()));
    }

    [Fact]
    public async Task Delete_WithoutScope_OnContainerOnlyClient_ThrowsInvalidOperation()
    {
        using HttpClient httpClient = new();
        using IHostedFileClient client = CreateContainerClient(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DeleteAsync("file-abc123"));
    }

    [Fact]
    public void GetService_ReturnsMetadata()
    {
        var client = new OpenAIClient(new ApiKeyCredential("key"));
        using IHostedFileClient fileClient = client.AsIHostedFileClient();

        var metadata = fileClient.GetService<HostedFileClientMetadata>();

        Assert.NotNull(metadata);
        Assert.Equal("openai", metadata.ProviderName);
    }

    [Fact]
    public void GetService_ReturnsSelfForIHostedFileClient()
    {
        var client = new OpenAIClient(new ApiKeyCredential("key"));
        using IHostedFileClient fileClient = client.AsIHostedFileClient();

        var self = fileClient.GetService<IHostedFileClient>();

        Assert.Same(fileClient, self);
    }

    [Fact]
    public void GetService_ReturnsOpenAIFileClient()
    {
        var openAIClient = new OpenAIClient(new ApiKeyCredential("key"));
        using IHostedFileClient fileClient = openAIClient.AsIHostedFileClient();

        var innerClient = fileClient.GetService<OpenAIFileClient>();

        Assert.NotNull(innerClient);
    }

    [Fact]
    public void GetService_ReturnsContainerClient()
    {
        var openAIClient = new OpenAIClient(new ApiKeyCredential("key"));
        using IHostedFileClient fileClient = openAIClient.AsIHostedFileClient();

        var innerClient = fileClient.GetService<ContainerClient>();

        Assert.NotNull(innerClient);
    }

    [Fact]
    public void GetService_ReturnsNullForUnknownType()
    {
        var client = new OpenAIClient(new ApiKeyCredential("key"));
        using IHostedFileClient fileClient = client.AsIHostedFileClient();

        var result = fileClient.GetService(typeof(string));

        Assert.Null(result);
    }

    [Fact]
    public void GetService_WithNonNullKey_ReturnsNull()
    {
        var client = new OpenAIClient(new ApiKeyCredential("key"));
        using IHostedFileClient fileClient = client.AsIHostedFileClient();

        var result = fileClient.GetService(typeof(HostedFileClientMetadata), "somekey");

        Assert.Null(result);
    }

    [Fact]
    public void GetService_FileOnlyClient_ReturnsNullForContainerClient()
    {
        var openAIClient = new OpenAIClient(new ApiKeyCredential("key"));
        using IHostedFileClient fileClient = openAIClient.GetOpenAIFileClient().AsIHostedFileClient();

        var innerClient = fileClient.GetService<ContainerClient>();

        Assert.Null(innerClient);
    }

    [Fact]
    public void GetService_ContainerOnlyClient_ReturnsNullForOpenAIFileClient()
    {
        var openAIClient = new OpenAIClient(new ApiKeyCredential("key"));
        using IHostedFileClient fileClient = openAIClient.GetContainerClient().AsIHostedFileClient();

        var innerClient = fileClient.GetService<OpenAIFileClient>();

        Assert.Null(innerClient);
    }

    [Fact]
    public async Task Download_ReturnsStreamWithMediaTypeAndFileName()
    {
        byte[] fileData = "Hello, World!"u8.ToArray();

        using var handler = new RoutingHandler(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/content", StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(fileData) }
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        {
                            "id": "file-abc123",
                            "object": "file",
                            "bytes": 13,
                            "created_at": 1613677385,
                            "filename": "hello.txt",
                            "purpose": "assistants"
                        }
                        """, Encoding.UTF8, "application/json")
                });
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        using var stream = await client.DownloadAsync("file-abc123");

        Assert.Equal("text/plain", stream.MediaType);
        Assert.Equal("hello.txt", stream.FileName);

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        Assert.Equal(fileData, ms.ToArray());
    }

    [Fact]
    public void GetService_NullServiceType_Throws()
    {
        var client = new OpenAIClient(new ApiKeyCredential("key"));
        using IHostedFileClient fileClient = client.AsIHostedFileClient();

        Assert.Throws<ArgumentNullException>("serviceType", () => fileClient.GetService(null!));
    }

    [Fact]
    public async Task Upload_WithScope_UsesContainerApi()
    {
        using var handler = new RoutingHandler(request =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                        "id": "cfile-abc",
                        "object": "container.file",
                        "path": "uploads/data.csv"
                    }
                    """, Encoding.UTF8, "application/json")
            });
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        using var contentStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await client.UploadAsync(contentStream, "text/csv", "data.csv", new HostedFileUploadOptions { Scope = "ctr-123" });

        Assert.Equal("cfile-abc", result.Id);
        Assert.Equal("data.csv", result.Name);
        Assert.Equal("text/csv", result.MediaType);
    }

    [Fact]
    public async Task Download_WithScope_UsesContainerApi()
    {
        byte[] fileData = "container content"u8.ToArray();

        using var handler = new RoutingHandler(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/content", StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(fileData) }
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        {
                            "id": "cfile-1",
                            "object": "container.file",
                            "path": "/uploads/data.txt"
                        }
                        """, Encoding.UTF8, "application/json")
                });
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        using var stream = await client.DownloadAsync("cfile-1", new HostedFileDownloadOptions { Scope = "ctr-123" });

        Assert.Equal("text/plain", stream.MediaType);
        Assert.Equal("data.txt", stream.FileName);

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        Assert.Equal(fileData, ms.ToArray());
    }

    [Fact]
    public async Task GetFileInfo_WithScope_ReturnsContainerFileInfo()
    {
        using var handler = new RoutingHandler(request =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                        "id": "cfile-1",
                        "object": "container.file",
                        "path": "/path/to/report.pdf"
                    }
                    """, Encoding.UTF8, "application/json")
            });
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        var result = await client.GetFileInfoAsync("cfile-1", new HostedFileGetOptions { Scope = "ctr-123" });

        Assert.NotNull(result);
        Assert.Equal("cfile-1", result.Id);
        Assert.Equal("report.pdf", result.Name);
        Assert.Equal("application/pdf", result.MediaType);
        Assert.NotNull(result.RawRepresentation);
    }

    [Fact]
    public async Task GetFileInfo_WithScope_NotFound_ReturnsNull()
    {
        using NotFoundHandler handler = new();
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        var result = await client.GetFileInfoAsync("cfile-nonexistent", new HostedFileGetOptions { Scope = "ctr-123" });

        Assert.Null(result);
    }

    [Fact]
    public async Task ListFiles_WithScope_ReturnsContainerFiles()
    {
        using var handler = new RoutingHandler(request =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                        "data": [
                            {"id": "cfile-1", "object": "container.file", "path": "file1.txt"},
                            {"id": "cfile-2", "object": "container.file", "path": "dir/file2.csv"}
                        ],
                        "object": "list",
                        "has_more": false,
                        "first_id": "cfile-1",
                        "last_id": "cfile-2"
                    }
                    """, Encoding.UTF8, "application/json")
            });
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        var files = await CollectAsync(client.ListFilesAsync(new HostedFileListOptions { Scope = "ctr-123" }));

        Assert.Equal(2, files.Count);

        Assert.Equal("cfile-1", files[0].Id);
        Assert.Equal("file1.txt", files[0].Name);
        Assert.Equal("text/plain", files[0].MediaType);
        Assert.NotNull(files[0].RawRepresentation);

        Assert.Equal("cfile-2", files[1].Id);
        Assert.Equal("file2.csv", files[1].Name);
        Assert.Equal("text/csv", files[1].MediaType);
        Assert.NotNull(files[1].RawRepresentation);
    }

    [Fact]
    public async Task Delete_WithScope_Success_ReturnsTrue()
    {
        using var handler = new RoutingHandler(request =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        var result = await client.DeleteAsync("cfile-1", new HostedFileDeleteOptions { Scope = "ctr-123" });

        Assert.True(result);
    }

    [Fact]
    public async Task Delete_WithScope_NotFound_ReturnsFalse()
    {
        using NotFoundHandler handler = new();
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateFileClient(httpClient);

        var result = await client.DeleteAsync("cfile-nonexistent", new HostedFileDeleteOptions { Scope = "ctr-123" });

        Assert.False(result);
    }

    [Fact]
    public async Task DefaultScope_IsUsedWhenNoScopeInOptions()
    {
        using var handler = new RoutingHandler(request =>
        {
            Assert.Contains("/containers/default-ctr/", request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                        "id": "cfile-1",
                        "object": "container.file",
                        "path": "test.txt"
                    }
                    """, Encoding.UTF8, "application/json")
            };
        });
        using HttpClient httpClient = new(handler);
        using IHostedFileClient client = CreateContainerClient(httpClient, defaultScope: "default-ctr");

        var result = await client.GetFileInfoAsync("cfile-1");

        Assert.NotNull(result);
        Assert.Equal("cfile-1", result.Id);
        Assert.Equal("test.txt", result.Name);
    }

    private static IHostedFileClient CreateFileClient(HttpClient httpClient) =>
        new OpenAIClient(new ApiKeyCredential("apikey"), new OpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) })
        .AsIHostedFileClient();

    private static IHostedFileClient CreateFileOnlyClient(HttpClient httpClient) =>
        new OpenAIClient(new ApiKeyCredential("apikey"), new OpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) })
        .GetOpenAIFileClient()
        .AsIHostedFileClient();

    private static IHostedFileClient CreateContainerClient(HttpClient httpClient, string? defaultScope = null) =>
        new OpenAIClient(new ApiKeyCredential("apikey"), new OpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) })
        .GetContainerClient()
        .AsIHostedFileClient(defaultScope);

    private sealed class NotFoundHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}") });
    }

    private sealed class RoutingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }

    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
        {
            list.Add(item);
        }

        return list;
    }
}
