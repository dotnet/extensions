// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Reads source content and converts it to an <see cref="IngestionDocument"/>.
/// </summary>
/// <typeparam name="TSource">The type of the source to read from. Sample: <see cref="FileInfo"/>, <see cref="Stream"/>, etc.</typeparam>
public interface IIngestionDocumentReader<TSource>
{
    /// <summary>
    /// Reads a source and converts it to an <see cref="IngestionDocument"/>.
    /// </summary>
    /// <param name="source">The source to read.</param>
    /// <param name="identifier">The unique identifier for the document.</param>
    /// <param name="mediaType">The media type of the file.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous read operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="identifier"/> is <see langword="null"/> or empty.</exception>
    Task<IngestionDocument> ReadAsync(TSource source, string identifier, string? mediaType = null, CancellationToken cancellationToken = default);
}
