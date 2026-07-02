// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
    public static Task<OcrResult> GetTextAsync(
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

        return client.GetTextAsync(documentStream, document.MediaType, options, progress, cancellationToken);
    }
}
