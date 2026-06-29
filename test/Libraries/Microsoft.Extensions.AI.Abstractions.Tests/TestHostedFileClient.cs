// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

internal sealed class TestHostedFileClient : IHostedFileClient
{
    public TestHostedFileClient()
    {
        GetServiceCallback = DefaultGetServiceCallback;
    }

    public Func<Stream, string?, string?, HostedFileClientOptions?, CancellationToken, Task<HostedFileContent>>? UploadAsyncCallback { get; set; }

    public Func<string, HostedFileClientOptions?, CancellationToken, Task<HostedFileDownloadStream>>? DownloadAsyncCallback { get; set; }

    public Func<string, HostedFileClientOptions?, CancellationToken, Task<HostedFileContent?>>? GetFileInfoAsyncCallback { get; set; }

    public Func<HostedFileClientOptions?, CancellationToken, IAsyncEnumerable<HostedFileContent>>? ListFilesAsyncCallback { get; set; }

    public Func<string, HostedFileClientOptions?, CancellationToken, Task<bool>>? DeleteAsyncCallback { get; set; }

    public Func<Type, object?, object?> GetServiceCallback { get; set; }

    private object? DefaultGetServiceCallback(Type serviceType, object? serviceKey) =>
        serviceType is not null && serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public Task<HostedFileContent> UploadAsync(
        Stream content,
        string? mediaType = null,
        string? fileName = null,
        HostedFileClientOptions? options = null,
        CancellationToken cancellationToken = default) =>
        UploadAsyncCallback!.Invoke(content, mediaType, fileName, options, cancellationToken);

    public Task<HostedFileDownloadStream> DownloadAsync(
        string fileId,
        HostedFileClientOptions? options = null,
        CancellationToken cancellationToken = default) =>
        DownloadAsyncCallback!.Invoke(fileId, options, cancellationToken);

    public Task<HostedFileContent?> GetFileInfoAsync(
        string fileId,
        HostedFileClientOptions? options = null,
        CancellationToken cancellationToken = default) =>
        GetFileInfoAsyncCallback!.Invoke(fileId, options, cancellationToken);

    public IAsyncEnumerable<HostedFileContent> ListFilesAsync(
        HostedFileClientOptions? options = null,
        CancellationToken cancellationToken = default) =>
        ListFilesAsyncCallback!.Invoke(options, cancellationToken);

    public Task<bool> DeleteAsync(
        string fileId,
        HostedFileClientOptions? options = null,
        CancellationToken cancellationToken = default) =>
        DeleteAsyncCallback!.Invoke(fileId, options, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        GetServiceCallback(serviceType, serviceKey);

    public bool IsDisposed { get; private set; }

    void IDisposable.Dispose()
    {
        IsDisposed = true;
    }
}
