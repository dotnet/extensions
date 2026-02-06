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

    public Func<Stream, string?, string?, HostedFileUploadOptions?, CancellationToken, Task<HostedFile>>? UploadAsyncCallback { get; set; }

    public Func<string, HostedFileDownloadOptions?, CancellationToken, Task<HostedFileDownloadStream>>? DownloadAsyncCallback { get; set; }

    public Func<string, HostedFileGetOptions?, CancellationToken, Task<HostedFile?>>? GetFileInfoAsyncCallback { get; set; }

    public Func<HostedFileListOptions?, CancellationToken, IAsyncEnumerable<HostedFile>>? ListFilesAsyncCallback { get; set; }

    public Func<string, HostedFileDeleteOptions?, CancellationToken, Task<bool>>? DeleteAsyncCallback { get; set; }

    public Func<Type, object?, object?> GetServiceCallback { get; set; }

    private object? DefaultGetServiceCallback(Type serviceType, object? serviceKey) =>
        serviceType is not null && serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public Task<HostedFile> UploadAsync(
        Stream content,
        string? mediaType = null,
        string? fileName = null,
        HostedFileUploadOptions? options = null,
        CancellationToken cancellationToken = default) =>
        UploadAsyncCallback!.Invoke(content, mediaType, fileName, options, cancellationToken);

    public Task<HostedFileDownloadStream> DownloadAsync(
        string fileId,
        HostedFileDownloadOptions? options = null,
        CancellationToken cancellationToken = default) =>
        DownloadAsyncCallback!.Invoke(fileId, options, cancellationToken);

    public Task<HostedFile?> GetFileInfoAsync(
        string fileId,
        HostedFileGetOptions? options = null,
        CancellationToken cancellationToken = default) =>
        GetFileInfoAsyncCallback!.Invoke(fileId, options, cancellationToken);

    public IAsyncEnumerable<HostedFile> ListFilesAsync(
        HostedFileListOptions? options = null,
        CancellationToken cancellationToken = default) =>
        ListFilesAsyncCallback!.Invoke(options, cancellationToken);

    public Task<bool> DeleteAsync(
        string fileId,
        HostedFileDeleteOptions? options = null,
        CancellationToken cancellationToken = default) =>
        DeleteAsyncCallback!.Invoke(fileId, options, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        GetServiceCallback(serviceType, serviceKey);

    void IDisposable.Dispose()
    {
        // No resources need disposing.
    }
}
