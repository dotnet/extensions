// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Represents options for the <see cref="VectorStoreWriter{T}"/>.
/// </summary>
public sealed class VectorStoreWriterOptions
{
    private string _collectionName = "chunks";

    /// <summary>
    /// Gets or sets the name of the collection. When not provided, "chunks" will be used.
    /// </summary>
    public string CollectionName
    {
        get => _collectionName;
        set => _collectionName = string.IsNullOrEmpty(value) ? throw new ArgumentNullException(nameof(value)) : value;
    }

    /// <summary>
    /// Gets or sets the distance function to use when creating the collection.
    /// </summary>
    /// <remarks>
    /// When not provided, the default specific to given database will be used. Check <see cref="VectorData.DistanceFunction"/> for available values.
    /// </remarks>
    public string? DistanceFunction { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to perform incremental ingestion.
    /// </summary>
    /// <remarks>
    /// When enabled, the writer will delete the chunks for the given document before inserting the new ones.
    /// So the ingestion will "replace" the existing chunks for the document with the new ones.
    /// </remarks>
    public bool IncrementalIngestion { get; set; }
}
