// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TestUtilities;
using Xunit;

#pragma warning disable MEAI001

namespace Microsoft.Extensions.AI;

public sealed class OpenAIHostedFileClientIntegrationTests : IDisposable
{
    private readonly IHostedFileClient? _client = IntegrationTestHelpers.GetOpenAIClient()?.AsIHostedFileClient();

    public void Dispose()
    {
        _client?.Dispose();
    }

    /// <summary>Retries a download operation to handle the delay between upload and file availability.</summary>
    private static async Task<T> RetryAsync<T>(Func<Task<T>> action, int maxRetries = 5, int delayMs = 2000)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return await action();
            }
            catch (ClientResultException) when (i < maxRetries - 1)
            {
                await Task.Delay(delayMs * (i + 1));
            }
        }

        return await action();
    }

    private void SkipIfNotEnabled()
    {
        string? skipIntegration = TestRunnerConfiguration.Instance["SkipIntegrationTests"];

        if (skipIntegration is not null || _client is null)
        {
            throw new SkipTestException("Client is not enabled.");
        }
    }

    [ConditionalFact]
    public async Task Upload_Download_Delete_Roundtrip()
    {
        SkipIfNotEnabled();

        string fileName = $"test-{Guid.NewGuid():N}.jsonl";
        string content = """{"prompt": "hello", "completion": "world"}""";
        byte[] contentBytes = Encoding.UTF8.GetBytes(content);
        string? uploadedFileId = null;

        try
        {
            // Upload with "fine-tune" purpose since "assistants" files cannot be downloaded via the Files API.
            using var uploadStream = new MemoryStream(contentBytes);
            var uploadedFile = await _client!.UploadAsync(uploadStream, "application/jsonl", fileName, new HostedFileUploadOptions { Purpose = "fine-tune" });
            uploadedFileId = uploadedFile.Id;

            Assert.NotNull(uploadedFile.Id);
            Assert.NotEmpty(uploadedFile.Id);
            Assert.Equal(fileName, uploadedFile.Name);
            Assert.Equal("application/x-ndjson", uploadedFile.MediaType);
            Assert.NotNull(uploadedFile.Purpose);
            Assert.NotNull(uploadedFile.SizeInBytes);
            Assert.Equal(contentBytes.Length, uploadedFile.SizeInBytes);
            Assert.NotNull(uploadedFile.CreatedAt);
            Assert.NotNull(uploadedFile.RawRepresentation);

            // Download (with retry - files may not be immediately available)
            string downloadedContent = await RetryAsync(async () =>
            {
                using var downloadStream = await _client.DownloadAsync(uploadedFileId);
                Assert.Equal("application/x-ndjson", downloadStream.MediaType);
                Assert.Equal(fileName, downloadStream.FileName);
                using var reader = new StreamReader(downloadStream, Encoding.UTF8);
                return await reader.ReadToEndAsync();
            });

            Assert.Equal(content, downloadedContent);

            // Delete
            bool deleted = await _client.DeleteAsync(uploadedFileId);
            Assert.True(deleted);
            uploadedFileId = null;

            // Verify not found after deletion
            var fileInfo = await _client.GetFileInfoAsync(uploadedFile.Id);
            Assert.Null(fileInfo);
        }
        finally
        {
            if (uploadedFileId is not null)
            {
                await _client!.DeleteAsync(uploadedFileId);
            }
        }
    }

    [ConditionalFact]
    public async Task Upload_ListFiles_VerifyPresent()
    {
        SkipIfNotEnabled();

        string fileName = $"test-{Guid.NewGuid():N}.txt";
        string? uploadedFileId = null;

        try
        {
            using var uploadStream = new MemoryStream("list test"u8.ToArray());
            var uploadedFile = await _client!.UploadAsync(uploadStream, "text/plain", fileName, new HostedFileUploadOptions { Purpose = "assistants" });
            uploadedFileId = uploadedFile.Id;

            var files = new List<HostedFile>();
            await foreach (var file in _client.ListFilesAsync(new HostedFileListOptions { Purpose = "assistants" }))
            {
                files.Add(file);
            }

            Assert.Contains(files, f => f.Id == uploadedFileId && f.MediaType == "text/plain" && f.Name == fileName);
        }
        finally
        {
            if (uploadedFileId is not null)
            {
                await _client!.DeleteAsync(uploadedFileId);
            }
        }
    }

    [ConditionalFact]
    public async Task GetFileInfo_ReturnsMetadata()
    {
        SkipIfNotEnabled();

        string fileName = $"test-{Guid.NewGuid():N}.txt";
        byte[] contentBytes = "metadata test"u8.ToArray();
        string? uploadedFileId = null;

        try
        {
            using var uploadStream = new MemoryStream(contentBytes);
            var uploadedFile = await _client!.UploadAsync(uploadStream, "text/plain", fileName, new HostedFileUploadOptions { Purpose = "assistants" });
            uploadedFileId = uploadedFile.Id;

            var fileInfo = await _client.GetFileInfoAsync(uploadedFileId);

            Assert.NotNull(fileInfo);
            Assert.Equal(uploadedFileId, fileInfo.Id);
            Assert.Equal(fileName, fileInfo.Name);
            Assert.Equal("text/plain", fileInfo.MediaType);
            Assert.NotNull(fileInfo.SizeInBytes);
            Assert.True(fileInfo.SizeInBytes > 0);
            Assert.NotNull(fileInfo.Purpose);
            Assert.NotNull(fileInfo.CreatedAt);
        }
        finally
        {
            if (uploadedFileId is not null)
            {
                await _client!.DeleteAsync(uploadedFileId);
            }
        }
    }

    [ConditionalFact]
    public async Task Delete_NonExistent_ReturnsFalse()
    {
        SkipIfNotEnabled();

        bool deleted = await _client!.DeleteAsync("file-nonexistent000000000000");
        Assert.False(deleted);
    }

    [ConditionalFact]
    public async Task GetFileInfo_NonExistent_ReturnsNull()
    {
        SkipIfNotEnabled();

        var fileInfo = await _client!.GetFileInfoAsync("file-nonexistent000000000000");
        Assert.Null(fileInfo);
    }

    [ConditionalFact]
    public async Task Upload_DataContent_Extension()
    {
        SkipIfNotEnabled();

        string fileName = $"test-{Guid.NewGuid():N}.txt";
        byte[] contentBytes = "data content test"u8.ToArray();
        string? uploadedFileId = null;

        try
        {
            var dataContent = new DataContent(contentBytes, "text/plain") { Name = fileName };
            var uploadedFile = await _client!.UploadAsync(dataContent, new HostedFileUploadOptions { Purpose = "assistants" });
            uploadedFileId = uploadedFile.Id;

            Assert.NotNull(uploadedFile.Id);
            Assert.NotEmpty(uploadedFile.Id);
            Assert.Equal("text/plain", uploadedFile.MediaType);
        }
        finally
        {
            if (uploadedFileId is not null)
            {
                await _client!.DeleteAsync(uploadedFileId);
            }
        }
    }

    [ConditionalFact]
    public async Task Download_AsDataContent_Extension()
    {
        SkipIfNotEnabled();

        string fileName = $"test-{Guid.NewGuid():N}.jsonl";
        string content = """{"prompt": "hello", "completion": "world"}""";
        byte[] contentBytes = Encoding.UTF8.GetBytes(content);
        string? uploadedFileId = null;

        try
        {
            using var uploadStream = new MemoryStream(contentBytes);
            var uploadedFile = await _client!.UploadAsync(uploadStream, "application/jsonl", fileName, new HostedFileUploadOptions { Purpose = "fine-tune" });
            uploadedFileId = uploadedFile.Id;

            var dataContent = await RetryAsync(() => _client.DownloadAsDataContentAsync(uploadedFileId));
            Assert.Equal(content, Encoding.UTF8.GetString(dataContent.Data.ToArray()));
            Assert.Equal("application/x-ndjson", dataContent.MediaType);
        }
        finally
        {
            if (uploadedFileId is not null)
            {
                await _client!.DeleteAsync(uploadedFileId);
            }
        }
    }

    [ConditionalFact]
    public async Task Upload_DownloadTo_Extension()
    {
        SkipIfNotEnabled();

        string fileName = $"test-{Guid.NewGuid():N}.jsonl";
        string content = """{"prompt": "hello", "completion": "world"}""";
        byte[] contentBytes = Encoding.UTF8.GetBytes(content);
        string? uploadedFileId = null;
        string? tempDir = null;

        try
        {
            using var uploadStream = new MemoryStream(contentBytes);
            var uploadedFile = await _client!.UploadAsync(uploadStream, "application/jsonl", fileName, new HostedFileUploadOptions { Purpose = "fine-tune" });
            uploadedFileId = uploadedFile.Id;

            tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string savedPath = await RetryAsync(() => _client.DownloadToAsync(uploadedFileId, tempDir));

            Assert.True(File.Exists(savedPath));
            string savedContent = File.ReadAllText(savedPath);
            Assert.Equal(content, savedContent);
        }
        finally
        {
            if (uploadedFileId is not null)
            {
                await _client!.DeleteAsync(uploadedFileId);
            }

            if (tempDir is not null && Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
