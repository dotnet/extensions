// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DelegatingHostedFileClientTests
{
    [Fact]
    public void RequiresInnerClient()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new NoOpDelegatingHostedFileClient(null!));
    }

    [Fact]
    public async Task UploadAsyncDefaultsToInnerClientAsync()
    {
        var expectedStream = new MemoryStream([1, 2, 3]);
        var expectedMediaType = "text/plain";
        var expectedFileName = "test.txt";
        var expectedOptions = new HostedFileUploadOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<HostedFile>();
        var expectedFile = new HostedFile("file-123");
        using var inner = new TestHostedFileClient
        {
            UploadAsyncCallback = (content, mediaType, fileName, options, cancellationToken) =>
            {
                Assert.Same(expectedStream, content);
                Assert.Equal(expectedMediaType, mediaType);
                Assert.Equal(expectedFileName, fileName);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingHostedFileClient(inner);
        var resultTask = delegating.UploadAsync(expectedStream, expectedMediaType, expectedFileName, expectedOptions, expectedCancellationToken);
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedFile);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedFile, await resultTask);
    }

    [Fact]
    public async Task DownloadAsyncDefaultsToInnerClientAsync()
    {
        var expectedFileId = "file-456";
        var expectedOptions = new HostedFileDownloadOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<HostedFileDownloadStream>();
        using var inner = new TestHostedFileClient
        {
            DownloadAsyncCallback = (fileId, options, cancellationToken) =>
            {
                Assert.Equal(expectedFileId, fileId);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingHostedFileClient(inner);
        var resultTask = delegating.DownloadAsync(expectedFileId, expectedOptions, expectedCancellationToken);
        Assert.False(resultTask.IsCompleted);
        using var downloadStream = new TestHostedFileDownloadStream([1, 2, 3]);
        expectedResult.SetResult(downloadStream);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(downloadStream, await resultTask);
    }

    [Fact]
    public async Task GetFileInfoAsyncDefaultsToInnerClientAsync()
    {
        var expectedFileId = "file-789";
        var expectedOptions = new HostedFileGetOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<HostedFile?>();
        var expectedFile = new HostedFile("file-789") { Name = "info.txt" };
        using var inner = new TestHostedFileClient
        {
            GetFileInfoAsyncCallback = (fileId, options, cancellationToken) =>
            {
                Assert.Equal(expectedFileId, fileId);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingHostedFileClient(inner);
        var resultTask = delegating.GetFileInfoAsync(expectedFileId, expectedOptions, expectedCancellationToken);
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedFile);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedFile, await resultTask);
    }

    [Fact]
    public async Task ListFilesAsyncDefaultsToInnerClientAsync()
    {
        var expectedOptions = new HostedFileListOptions();
        var expectedCancellationToken = CancellationToken.None;
        HostedFile[] expectedFiles =
        [
            new HostedFile("file-1"),
            new HostedFile("file-2")
        ];

        using var inner = new TestHostedFileClient
        {
            ListFilesAsyncCallback = (options, cancellationToken) =>
            {
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return YieldAsync(expectedFiles);
            }
        };

        using var delegating = new NoOpDelegatingHostedFileClient(inner);
        var resultAsyncEnumerable = delegating.ListFilesAsync(expectedOptions, expectedCancellationToken);
        var enumerator = resultAsyncEnumerable.GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Same(expectedFiles[0], enumerator.Current);
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Same(expectedFiles[1], enumerator.Current);
        Assert.False(await enumerator.MoveNextAsync());
    }

    [Fact]
    public async Task DeleteAsyncDefaultsToInnerClientAsync()
    {
        var expectedFileId = "file-del";
        var expectedOptions = new HostedFileDeleteOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<bool>();
        using var inner = new TestHostedFileClient
        {
            DeleteAsyncCallback = (fileId, options, cancellationToken) =>
            {
                Assert.Equal(expectedFileId, fileId);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingHostedFileClient(inner);
        var resultTask = delegating.DeleteAsync(expectedFileId, expectedOptions, expectedCancellationToken);
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(true);
        Assert.True(resultTask.IsCompleted);
        Assert.True(await resultTask);
    }

    [Fact]
    public void GetServiceThrowsForNullType()
    {
        using var inner = new TestHostedFileClient();
        using var delegating = new NoOpDelegatingHostedFileClient(inner);
        Assert.Throws<ArgumentNullException>("serviceType", () => delegating.GetService(null!));
    }

    [Fact]
    public void GetServiceReturnsSelfForIHostedFileClientType()
    {
        using var inner = new TestHostedFileClient();
        using var delegating = new NoOpDelegatingHostedFileClient(inner);
        var client = delegating.GetService(typeof(IHostedFileClient));
        Assert.Same(delegating, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerForOtherTypes()
    {
        var expectedResult = TimeZoneInfo.Local;
        var expectedKey = new object();
        using var inner = new TestHostedFileClient
        {
            GetServiceCallback = (type, key) => type == typeof(TimeZoneInfo) && key == expectedKey
                ? expectedResult
                : throw new InvalidOperationException("Unexpected call")
        };
        using var delegating = new NoOpDelegatingHostedFileClient(inner);
        var tzi = delegating.GetService(typeof(TimeZoneInfo), expectedKey);
        Assert.Same(expectedResult, tzi);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfKeyIsNotNull()
    {
        var expectedKey = new object();
        var expectedResult = "some-service";
        using var inner = new TestHostedFileClient
        {
            GetServiceCallback = (type, key) => type == typeof(string) && key == expectedKey
                ? expectedResult
                : throw new InvalidOperationException("Unexpected call")
        };
        using var delegating = new NoOpDelegatingHostedFileClient(inner);
        var result = delegating.GetService(typeof(string), expectedKey);
        Assert.Same(expectedResult, result);
    }

    private static async IAsyncEnumerable<T> YieldAsync<T>(IEnumerable<T> input)
    {
        await Task.Yield();
        foreach (var item in input)
        {
            yield return item;
        }
    }

    private sealed class NoOpDelegatingHostedFileClient(IHostedFileClient innerClient)
        : DelegatingHostedFileClient(innerClient);

    private sealed class TestHostedFileDownloadStream : HostedFileDownloadStream
    {
        private readonly MemoryStream _inner;

        public TestHostedFileDownloadStream(byte[] data)
        {
            _inner = new MemoryStream(data);
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
