// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the result of an operation to generate embeddings.</summary>
/// <typeparam name="TEmbedding">Specifies the type of the generated embeddings.</typeparam>
public sealed class GeneratedEmbeddings<TEmbedding> : IList<TEmbedding>, IReadOnlyList<TEmbedding>
    where TEmbedding : Embedding
{
    /// <summary>The underlying list of embeddings.</summary>
    private List<TEmbedding> _embeddings;

    /// <summary>Initializes a new instance of the <see cref="GeneratedEmbeddings{TEmbedding}"/> class.</summary>
    public GeneratedEmbeddings()
    {
        _embeddings = [];
    }

    /// <summary>Initializes a new instance of the <see cref="GeneratedEmbeddings{TEmbedding}"/> class with the specified capacity.</summary>
    /// <param name="capacity">The number of embeddings that the new list can initially store.</param>
    public GeneratedEmbeddings(int capacity)
    {
        _embeddings = new List<TEmbedding>(Throw.IfLessThan(capacity, 0));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratedEmbeddings{TEmbedding}"/> class that contains all of the embeddings from the specified collection.
    /// </summary>
    /// <param name="embeddings">The collection whose embeddings are copied to the new list.</param>
    public GeneratedEmbeddings(IEnumerable<TEmbedding> embeddings)
    {
        _embeddings = new List<TEmbedding>(Throw.IfNull(embeddings));
    }

    /// <summary>Gets or sets usage details for the embeddings' generation.</summary>
    public UsageDetails? Usage { get; set; }

    /// <summary>Gets or sets any additional properties associated with the embeddings.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <inheritdoc />
    public TEmbedding this[int index]
    {
        get => _embeddings[index];
        set => _embeddings[index] = value;
    }

    /// <inheritdoc />
    public int Count => _embeddings.Count;

    /// <inheritdoc />
    bool ICollection<TEmbedding>.IsReadOnly => false;

    /// <inheritdoc />
    public void Add(TEmbedding item) => _embeddings.Add(item);

    /// <summary>Adds the embeddings from the specified collection to the end of this list.</summary>
    /// <param name="items">The collection whose elements should be added to this list.</param>
    public void AddRange(IEnumerable<TEmbedding> items) => _embeddings.AddRange(items);

    /// <inheritdoc />
    public void Clear() => _embeddings.Clear();

    /// <inheritdoc />
    public bool Contains(TEmbedding item) => _embeddings.Contains(item);

    /// <inheritdoc />
    public void CopyTo(TEmbedding[] array, int arrayIndex) => _embeddings.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<TEmbedding> GetEnumerator() => _embeddings.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public int IndexOf(TEmbedding item) => _embeddings.IndexOf(item);

    /// <inheritdoc />
    public void Insert(int index, TEmbedding item) => _embeddings.Insert(index, item);

    /// <inheritdoc />
    public bool Remove(TEmbedding item) => _embeddings.Remove(item);

    /// <inheritdoc />
    public void RemoveAt(int index) => _embeddings.RemoveAt(index);
}
