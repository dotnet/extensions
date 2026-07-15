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

/// <summary>Represents an optical character recognition (OCR) / document-parsing client.</summary>
/// <remarks>
/// <para>
/// An <see cref="IOcrClient"/> transcribes a document or image into structured output: markdown,
/// per-page content, tables, layout blocks with bounding regions, and confidence. It is the
/// capability sibling to <see cref="IChatClient"/>, <c>IEmbeddingGenerator</c>, and
/// <c>ISpeechToTextClient</c> for the document-extraction problem.
/// </para>
/// <para>
/// The contract is independent of <see cref="IChatClient"/>. Most OCR / document-AI engines are not
/// chat models: they emit structured output (tables, bounding regions, confidence, reading order)
/// that does not map onto a chat response. An implementation may wrap a vision-capable
/// <see cref="IChatClient"/> as the lowest-fidelity, transcription-only path, but the interface does
/// not require one.
/// </para>
/// <para>
/// Unless otherwise specified, all members of <see cref="IOcrClient"/> are thread-safe for concurrent
/// use. Implementations might mutate the <see cref="OcrOptions"/> supplied to <see cref="ExtractAsync"/>
/// and <see cref="ExtractStreamingAsync"/>; consumers should avoid sharing a single options instance across
/// concurrent invocations when that is a concern. The document stream passed to these methods is not
/// disposed by the implementation.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public interface IOcrClient : IDisposable
{
    /// <summary>Runs OCR / document parsing over a document stream and returns structured output.</summary>
    /// <param name="document">The document or image content to parse.</param>
    /// <param name="mediaType">The media type of <paramref name="document"/>, for example <c>application/pdf</c> or <c>image/png</c>.</param>
    /// <param name="options">The OCR options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The structured OCR result.</returns>
    Task<OcrResult> ExtractAsync(
        Stream document,
        string mediaType,
        OcrOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Runs OCR / document parsing over a document stream and streams back structured output as it is produced.</summary>
    /// <param name="document">The document or image content to parse.</param>
    /// <param name="mediaType">The media type of <paramref name="document"/>, for example <c>application/pdf</c> or <c>image/png</c>.</param>
    /// <param name="options">The OCR options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The structured OCR updates representing the streamed output.</returns>
    /// <remarks>
    /// Engines that produce pages incrementally (for example, while polling a long-running operation such as
    /// Azure Document Intelligence) can yield each page as it completes, letting a consumer begin processing
    /// early pages before later pages finish. Synchronous engines may yield a single terminal update. Use
    /// <see cref="OcrResponseUpdateExtensions.ToOcrResultAsync"/> to reassemble the stream into an
    /// <see cref="OcrResult"/>.
    /// </remarks>
    IAsyncEnumerable<OcrResponseUpdate> ExtractStreamingAsync(
        Stream document,
        string mediaType,
        OcrOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Asks the <see cref="IOcrClient"/> for an object of the specified type <paramref name="serviceType"/>.</summary>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that might be
    /// provided by the <see cref="IOcrClient"/>, including itself or any services it might be wrapping.
    /// </remarks>
    object? GetService(Type serviceType, object? serviceKey = null);
}
