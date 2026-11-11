// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

namespace Microsoft.Extensions.VectorData;

/// <summary>
/// Describes the properties of a record in a vector store collection.
/// </summary>
/// <remarks>
/// Each property contains additional information about how the property will be treated by the vector store.
/// </remarks>
public sealed class VectorStoreCollectionDefinition
{
    /// <summary>
    /// Gets or sets the list of properties that are stored in the record.
    /// </summary>
    [AllowNull]
    public IList<VectorStoreProperty> Properties
    {
        get => field ??= [];
        set;
    }

    /// <summary>
    /// Gets or sets the default embedding generator for vector properties in this collection.
    /// </summary>
    public IEmbeddingGenerator? EmbeddingGenerator { get; set; }
}
