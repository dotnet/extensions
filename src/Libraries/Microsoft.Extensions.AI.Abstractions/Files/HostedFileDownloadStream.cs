// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a stream for downloading file content from an AI service.
/// </summary>
/// <remarks>
/// <para>
/// This abstract class extends <see cref="Stream"/> to provide additional metadata
/// about the downloaded file, such as its media type and file name. Implementations
/// should override the abstract <see cref="Stream"/> members and optionally override
/// <see cref="MediaType"/> and <see cref="FileName"/> to provide file metadata.
/// </para>
/// <para>
/// The <see cref="ToDataContentAsync"/> method provides a convenient way to buffer
/// the entire stream content into a <see cref="DataContent"/> instance.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIFiles, UrlFormat = DiagnosticIds.UrlFormat)]
public abstract class HostedFileDownloadStream : Stream
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostedFileDownloadStream"/> class.
    /// </summary>
    protected HostedFileDownloadStream()
    {
    }

    /// <summary>
    /// Gets the media type (MIME type) of the file content.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> if the media type is not known.
    /// </remarks>
    public virtual string? MediaType => null;

    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> if the file name is not known.
    /// </remarks>
    public virtual string? FileName => null;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void WriteByte(byte value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public override void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();

    /// <summary>
    /// Reads the entire stream content from its current position and returns it as a <see cref="DataContent"/>.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="DataContent"/> containing the buffered file content.</returns>
    /// <remarks>
    /// This method buffers the entire stream content into memory. For large files,
    /// consider streaming directly to the destination instead.
    /// </remarks>
    public virtual async Task<DataContent> ToDataContentAsync(CancellationToken cancellationToken = default)
    {
        MemoryStream memoryStream = new();

        await CopyToAsync(memoryStream,
#if !NET
            81920,
#endif
            cancellationToken).ConfigureAwait(false);

        return new DataContent(
            memoryStream.GetBuffer().AsMemory(0, (int)memoryStream.Length),
            MediaType ?? "application/octet-stream")
        {
            Name = FileName,
        };
    }

#if NET
    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();

    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
#endif
}
