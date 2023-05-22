// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Cloud.DocumentDb;

/// <summary>
/// Batch operation item to be used in transactional operations like <see cref="IDocumentWriter{TDocument}.ExecuteTransactionalBatchAsync"/>.
/// </summary>
/// <typeparam name="T">The type of the item the response contains.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types",
    Justification = "Not to be used as a key in key value structures.")]
public readonly struct BatchItem<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchItem{T}"/> struct.
    /// </summary>
    /// <param name="operation">The operation.</param>
    /// <param name="item">The document.</param>
    /// <param name="id">The document id.</param>
    /// <param name="itemVersion">The item version.</param>
    public BatchItem(
        BatchOperation operation,
        T? item = default,
        string? id = null,
        string? itemVersion = null)
    {
        Operation = operation;
        Item = item;
        Id = id;
        ItemVersion = itemVersion;
    }

    /// <summary>
    /// Gets the operation for this item.
    /// </summary>
    public BatchOperation Operation { get; }

    /// <summary>
    /// Gets the batch item payload.
    /// </summary>
    public T? Item { get; }

    /// <summary>
    /// Gets the item id required for operation.
    /// </summary>
    public string? Id { get; }

    /// <summary>
    /// Gets the item version for if match condition.
    /// </summary>
    /// <remarks>
    /// For HTTP based protocols it could be based on ETag property.
    /// It can be obtained from <see cref="IDatabaseResponse.ItemVersion"/>
    /// by doing operation providing item as result.
    /// </remarks>
    public string? ItemVersion { get; }
}
