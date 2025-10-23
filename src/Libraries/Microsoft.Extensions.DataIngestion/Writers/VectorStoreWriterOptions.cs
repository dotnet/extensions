// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DataIngestion;

public sealed class VectorStoreWriterOptions
{
    private string _collectionName = "chunks";

    /// <summary>
    /// The name of the collection. When not provided, "chunks" will be used.
    /// </summary>
    public string CollectionName
    {
        get => _collectionName;
        set => _collectionName = string.IsNullOrEmpty(value) ? throw new ArgumentNullException(nameof(value)) : value;
    }

    /// <summary>
    /// The distance function to use when creating the collection. When not provided, the default specific to given database will be used. Check <see cref="VectorData.DistanceFunction"/> for available values.
    /// </summary>
    public string? DistanceFunction { get; set; }

    /// <summary>
    /// When enabled, the writer will delete the chunks for the given document before inserting the new ones.
    /// So the ingestion will "replace" the existing chunks for the document with the new ones.
    /// </summary>
    public bool IncrementalIngestion { get; set; } = false;
}
