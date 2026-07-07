// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for <see cref="IOcrClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public static class OcrClientExtensions
{
    /// <summary>Asks the <see cref="IOcrClient"/> for an object of type <typeparamref name="TService"/>.</summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="client">The client.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that might be
    /// provided by the <see cref="IOcrClient"/>, including itself or any services it might be wrapping.
    /// </remarks>
    public static TService? GetService<TService>(this IOcrClient client, object? serviceKey = null)
    {
        _ = Throw.IfNull(client);

        return (TService?)client.GetService(typeof(TService), serviceKey);
    }

    /// <summary>Runs OCR over a single document provided as a <see cref="DataContent"/>.</summary>
    /// <param name="client">The client.</param>
    /// <param name="document">The document content to parse.</param>
    /// <param name="options">The OCR options to configure the request.</param>
    /// <param name="progress">An optional progress reporter.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The structured OCR result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="document"/> is <see langword="null"/>.</exception>
    public static Task<OcrResult> ExtractAsync(
        this IOcrClient client,
        DataContent document,
        OcrOptions? options = null,
        IProgress<OcrProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(document);

        var documentStream = MemoryMarshal.TryGetArray(document.Data, out var array) ?
            new MemoryStream(array.Array!, array.Offset, array.Count) :
            new MemoryStream(document.Data.ToArray());

        return client.ExtractAsync(documentStream, document.MediaType, options, progress, cancellationToken);
    }

    /// <summary>Runs OCR over a single document referenced by a <see cref="UriContent"/>.</summary>
    /// <param name="client">The client.</param>
    /// <param name="document">The document reference to parse.</param>
    /// <param name="options">The OCR options to configure the request.</param>
    /// <param name="progress">An optional progress reporter.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The structured OCR result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="document"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The <paramref name="document"/> references a remote URI, which this overload does not fetch.</exception>
    /// <remarks>
    /// This overload handles self-contained <c>data:</c> URIs by delegating to the
    /// <see cref="DataContent"/> overload. It intentionally does no file or network IO: for
    /// <c>file:</c> and remote (<c>http</c>/<c>https</c>) URIs it throws, because whether to read/download
    /// the bytes or hand the URL to the engine natively (for example Mistral <c>document_url</c> or Azure
    /// Document Intelligence <c>uriSource</c>) is an open design question the abstraction does not decide.
    /// To download a remote document explicitly, use
    /// <see cref="ExtractFromUriAsync(IOcrClient, UriContent, HttpClient, OcrOptions?, IProgress{OcrProgress}?, CancellationToken)"/>
    /// with a caller-supplied <see cref="HttpClient"/>; or read the bytes yourself and pass a
    /// <see cref="Stream"/> or <see cref="DataContent"/>; or use an engine that accepts a URL directly.
    /// </remarks>
    public static Task<OcrResult> ExtractAsync(
        this IOcrClient client,
        UriContent document,
        OcrOptions? options = null,
        IProgress<OcrProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(document);

        Uri uri = document.Uri;
        if (uri.IsAbsoluteUri && string.Equals(uri.Scheme, "data", StringComparison.OrdinalIgnoreCase))
        {
            // Reuse DataContent's data: URI parsing, then defer to the DataContent overload.
            return client.ExtractAsync(new DataContent(uri), options, progress, cancellationToken);
        }

        throw new NotSupportedException(
            "This overload handles only self-contained data: URIs. For file or remote URIs, read the bytes " +
            "and pass a stream or DataContent, or use an engine that accepts a URL natively.");
    }

    /// <summary>
    /// Runs OCR over a single document referenced by a <see cref="UriContent"/>, explicitly downloading the
    /// bytes with a caller-supplied <see cref="HttpClient"/> when the reference is remote.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="document">The document reference to download and parse.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> used to download remote (<c>http</c>/<c>https</c>) documents.</param>
    /// <param name="options">The OCR options to configure the request.</param>
    /// <param name="progress">An optional progress reporter.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The structured OCR result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/>, <paramref name="document"/>, or <paramref name="httpClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The <paramref name="document"/> uses a scheme other than <c>data:</c>, <c>http</c>, or <c>https</c>.</exception>
    /// <remarks>
    /// This is the explicit, opt-in counterpart to
    /// <see cref="ExtractAsync(IOcrClient, UriContent, OcrOptions?, IProgress{OcrProgress}?, CancellationToken)"/>,
    /// which never touches the network. Self-contained <c>data:</c> URIs are handled inline (no download);
    /// <c>http</c>/<c>https</c> URIs are fetched with the supplied <paramref name="httpClient"/> and passed
    /// to the stream-based extraction path. The abstraction performs no ambient network IO: the caller owns
    /// the <see cref="HttpClient"/> and therefore its handlers, authentication, timeouts, and lifetime.
    /// Engines that accept a URL natively (for example Azure Document Intelligence <c>uriSource</c>) should
    /// expose that on the concrete client instead; this extension serves the bytes-only majority.
    /// </remarks>
    public static async Task<OcrResult> ExtractFromUriAsync(
        this IOcrClient client,
        UriContent document,
        HttpClient httpClient,
        OcrOptions? options = null,
        IProgress<OcrProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(document);
        _ = Throw.IfNull(httpClient);

        Uri uri = document.Uri;

        // Self-contained data: URIs carry their own bytes - no download needed.
        if (uri.IsAbsoluteUri && string.Equals(uri.Scheme, "data", StringComparison.OrdinalIgnoreCase))
        {
            return await client.ExtractAsync(document, options, progress, cancellationToken).ConfigureAwait(false);
        }

        if (!uri.IsAbsoluteUri ||
            (!string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase)))
        {
            throw new NotSupportedException(
                "ExtractFromUriAsync downloads only http/https URIs (and inlines data: URIs). For other " +
                "schemes, read the bytes and pass a stream or DataContent.");
        }

        using HttpResponseMessage response = await httpClient
            .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();

        string mediaType = response.Content.Headers.ContentType?.MediaType ?? document.MediaType;

#if NET
        using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
        using Stream contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
        return await client.ExtractAsync(contentStream, mediaType, options, progress, cancellationToken).ConfigureAwait(false);
    }
}
