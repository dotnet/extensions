// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Represents a chunk of content extracted from an <see cref="IngestionDocument"/>.
/// </summary>
/// <typeparam name="T">The type of the content.</typeparam>
[DebuggerDisplay("{Content}")]
public sealed class IngestionChunk<T>
{
    private Dictionary<string, object>? _metadata;

    /// <summary>
    /// Gets the content of the chunk.
    /// </summary>
    public T Content { get; }

    /// <summary>
    /// Gets the document from which this chunk was extracted.
    /// </summary>
    public IngestionDocument Document { get; }

    /// <summary>
    /// Gets additional context for the chunk.
    /// </summary>
    public string? Context { get; }

    /// <summary>
    /// Gets a value indicating whether this chunk has metadata.
    /// </summary>
    public bool HasMetadata => _metadata?.Count > 0;

    /// <summary>
    /// Gets the metadata associated with this chunk.
    /// </summary>
    public IDictionary<string, object> Metadata => _metadata ??= [];

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionChunk{T}"/> class.
    /// </summary>
    /// <param name="content">The content of the chunk.</param>
    /// <param name="document">The document from which this chunk was extracted.</param>
    /// <param name="context">Additional context for the chunk.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="content"/> or <paramref name="document"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="content"/> is a string that is empty or contains only white-space characters.
    /// </exception>
    public IngestionChunk(T content, IngestionDocument document, string? context = null)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }
        if (typeof(T) == typeof(string))
        {
            if (string.IsNullOrWhiteSpace((string)(object)content))
            {
                throw new ArgumentException("Content cannot be null or whitespace.", nameof(content));
            }
        }

        Content = content;
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Context = context;
    }
}
