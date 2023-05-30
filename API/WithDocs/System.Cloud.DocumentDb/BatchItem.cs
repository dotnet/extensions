// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

/// <summary>
/// Batch operation item to be used in transactional operations like <see cref="M:System.Cloud.DocumentDb.IDocumentWriter`1.ExecuteTransactionalBatchAsync(System.Cloud.DocumentDb.RequestOptions{`0},System.Collections.Generic.IReadOnlyList{System.Cloud.DocumentDb.BatchItem{`0}},System.Threading.CancellationToken)" />.
/// </summary>
/// <typeparam name="T">The type of the item the response contains.</typeparam>
public readonly struct BatchItem<T>
{
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
    /// It can be obtained from <see cref="P:System.Cloud.DocumentDb.IDatabaseResponse.ItemVersion" />
    /// by doing operation providing item as result.
    /// </remarks>
    public string? ItemVersion { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.BatchItem`1" /> struct.
    /// </summary>
    /// <param name="operation">The operation.</param>
    /// <param name="item">The document.</param>
    /// <param name="id">The document id.</param>
    /// <param name="itemVersion">The item version.</param>
    public BatchItem(BatchOperation operation, T? item = default(T?), string? id = null, string? itemVersion = null);
}
