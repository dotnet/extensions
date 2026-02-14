// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// A delegating file client that wraps an inner <see cref="IHostedFileClient"/>.
/// </summary>
/// <remarks>
/// This class provides a base for creating file clients that modify or enhance the behavior
/// of another <see cref="IHostedFileClient"/>. By default, all methods delegate to the inner client.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIFiles, UrlFormat = DiagnosticIds.UrlFormat)]
public class DelegatingHostedFileClient : IHostedFileClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingHostedFileClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner client to delegate to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    protected DelegatingHostedFileClient(IHostedFileClient innerClient)
    {
        InnerClient = Throw.IfNull(innerClient);
    }

    /// <summary>Gets the inner <see cref="IHostedFileClient"/>.</summary>
    protected IHostedFileClient InnerClient { get; }

    /// <inheritdoc />
    public virtual Task<HostedFile> UploadAsync(
        Stream content,
        string? mediaType = null,
        string? fileName = null,
        HostedFileUploadOptions? options = null,
        CancellationToken cancellationToken = default) =>
        InnerClient.UploadAsync(content, mediaType, fileName, options, cancellationToken);

    /// <inheritdoc />
    public virtual Task<HostedFileDownloadStream> DownloadAsync(
        string fileId,
        HostedFileDownloadOptions? options = null,
        CancellationToken cancellationToken = default) =>
        InnerClient.DownloadAsync(fileId, options, cancellationToken);

    /// <inheritdoc />
    public virtual Task<HostedFile?> GetFileInfoAsync(
        string fileId,
        HostedFileGetOptions? options = null,
        CancellationToken cancellationToken = default) =>
        InnerClient.GetFileInfoAsync(fileId, options, cancellationToken);

    /// <inheritdoc />
    public virtual IAsyncEnumerable<HostedFile> ListFilesAsync(
        HostedFileListOptions? options = null,
        CancellationToken cancellationToken = default) =>
        InnerClient.ListFilesAsync(options, cancellationToken);

    /// <inheritdoc />
    public virtual Task<bool> DeleteAsync(
        string fileId,
        HostedFileDeleteOptions? options = null,
        CancellationToken cancellationToken = default) =>
        InnerClient.DeleteAsync(fileId, options, cancellationToken);

    /// <inheritdoc />
    public virtual object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        // If the key is non-null, we don't know what it means so pass through to the inner service.
        return
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this :
            InnerClient.GetService(serviceType, serviceKey);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the instance.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> if being called from <see cref="Dispose()"/>; otherwise, <see langword="false"/>.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        // By default, do not dispose the inner client, as it may be shared.
    }
}
