// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a client for uploading, downloading, and managing files hosted by an AI service.
/// </summary>
/// <remarks>
/// <para>
/// File clients enable interaction with server-side file storage used by AI services,
/// particularly for code interpreter inputs and outputs. Files uploaded through this
/// interface can be referenced in AI requests using <see cref="HostedFileContent"/>.
/// </para>
/// <para>
/// Unless otherwise specified, all members of <see cref="IHostedFileClient"/> are thread-safe
/// for concurrent use. It is expected that all implementations of <see cref="IHostedFileClient"/>
/// support being used by multiple requests concurrently. Instances must not be disposed
/// of while the instance is still in use.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIFiles, UrlFormat = DiagnosticIds.UrlFormat)]
public interface IHostedFileClient : IDisposable
{
    /// <summary>
    /// Uploads a file to the AI service.
    /// </summary>
    /// <param name="content">The stream containing the file content to upload.</param>
    /// <param name="mediaType">The media type (MIME type) of the content.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="options">Options to configure the upload.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>Information about the uploaded file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="content"/> is <see langword="null"/>.</exception>
    Task<HostedFile> UploadAsync(
        Stream content,
        string? mediaType = null,
        string? fileName = null,
        HostedFileUploadOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from the AI service.
    /// </summary>
    /// <param name="fileId">The ID of the file to download.</param>
    /// <param name="options">Options to configure the download.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="HostedFileDownloadStream"/> containing the file content. The stream should be disposed when no longer needed.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="fileId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="fileId"/> is empty or whitespace.</exception>
    Task<HostedFileDownloadStream> DownloadAsync(
        string fileId,
        HostedFileDownloadOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata about a file.
    /// </summary>
    /// <param name="fileId">The ID of the file.</param>
    /// <param name="options">Options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>Information about the file, or <see langword="null"/> if not found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fileId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="fileId"/> is empty or whitespace.</exception>
    Task<HostedFile?> GetFileInfoAsync(
        string fileId,
        HostedFileGetOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files accessible by this client.
    /// </summary>
    /// <param name="options">Options to configure the listing.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>An async enumerable of file information.</returns>
    IAsyncEnumerable<HostedFile> ListFilesAsync(
        HostedFileListOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the AI service.
    /// </summary>
    /// <param name="fileId">The ID of the file to delete.</param>
    /// <param name="options">Options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the file was deleted; <see langword="false"/> if the file was not found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fileId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="fileId"/> is empty or whitespace.</exception>
    Task<bool> DeleteAsync(
        string fileId,
        HostedFileDeleteOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks the <see cref="IHostedFileClient"/> for an object of the specified type <paramref name="serviceType"/>.
    /// </summary>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that might be provided by the <see cref="IHostedFileClient"/>,
    /// including itself or any services it might be wrapping. For example, to access the <see cref="HostedFileClientMetadata"/> for the instance,
    /// <see cref="GetService"/> may be used to request it.
    /// </remarks>
    object? GetService(Type serviceType, object? serviceKey = null);
}
