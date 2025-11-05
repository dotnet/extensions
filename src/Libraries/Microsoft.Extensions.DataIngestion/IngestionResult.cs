// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Represents the result of an ingestion operation.
/// </summary>
public sealed class IngestionResult
{
    /// <summary>
    /// Gets the source file that was ingested.
    /// </summary>
    public FileInfo Source { get; }

    /// <summary>
    /// Gets the ingestion document created from the source file, if reading the document has succeeded.
    /// </summary>
    public IngestionDocument? Document { get; }

    /// <summary>
    /// Gets the exception that occurred during ingestion, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets a value indicating whether the ingestion succeeded.
    /// </summary>
    public bool Succeeded => Exception is null;

    internal IngestionResult(FileInfo source, IngestionDocument? document, Exception? exception)
    {
        Source = Throw.IfNull(source);
        Document = document;
        Exception = exception;
    }
}
