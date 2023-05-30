// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.DocumentDb;

/// <summary>
/// An interface to clients for all document oriented (or NoSQL) databases for document write operations.
/// https://en.wikipedia.org/wiki/Document-oriented_database.
/// </summary>
/// <typeparam name="TDocument">
/// The document entity type to be used as a table schema.
/// Request results will be mapped to instance of this type.
/// </typeparam>
public interface IDocumentWriter<TDocument> where TDocument : notnull
{
    /// <summary>
    /// Patches a document.
    /// </summary>
    /// <param name="options">The request options.</param>
    /// <param name="id">The document id requested to patched.</param>
    /// <param name="patchOperations">The patch operations to be applied.</param>
    /// <param name="filter">The predicate filter to be checked before patch applied.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> containing a <see cref="T:System.Cloud.DocumentDb.IDatabaseResponse`1" /> which wraps the result document.</returns>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    Task<IDatabaseResponse<TDocument>> PatchDocumentAsync(RequestOptions<TDocument> options, string id, IReadOnlyList<PatchOperation> patchOperations, string? filter, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a document provided at <see cref="P:System.Cloud.DocumentDb.RequestOptions`1.Document" />.
    /// </summary>
    /// <remarks>
    /// The request will fail if an item already exists.
    /// </remarks>
    /// <param name="options">The request options.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> containing a <see cref="T:System.Cloud.DocumentDb.IDatabaseResponse`1" /> which wraps the created document.</returns>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    Task<IDatabaseResponse<TDocument>> CreateDocumentAsync(RequestOptions<TDocument> options, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces a document having provided id with <see cref="P:System.Cloud.DocumentDb.RequestOptions`1.Document" />.
    /// </summary>
    /// <remarks>
    /// The request will fail if a document having the provided id does not exist.
    /// If the id in the document different from the id provided in <see cref="P:System.Cloud.DocumentDb.RequestOptions`1.Document" />,
    /// the id will be updated too. After the operation succeed there will be no document with the old id anymore.
    /// </remarks>
    /// <param name="options">The request options.</param>
    /// <param name="id">Id of the document to replace.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> containing a <see cref="T:System.Cloud.DocumentDb.IDatabaseResponse`1" /> which wraps the updated document.</returns>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    Task<IDatabaseResponse<TDocument>> ReplaceDocumentAsync(RequestOptions<TDocument> options, string id, CancellationToken cancellationToken);

    /// <summary>
    /// Tries to insert or update a document with provided document id with value provided in <see cref="P:System.Cloud.DocumentDb.RequestOptions`1.Document" />.
    /// </summary>
    /// <remarks>
    /// This function should only be called if existence status of the target item is unknown.
    /// This is different from <see cref="M:System.Cloud.DocumentDb.IDocumentWriter`1.UpsertDocumentAsync(System.Cloud.DocumentDb.RequestOptions{`0},System.Threading.CancellationToken)" /> by providing a method for resolving conflicts.
    /// If the id in the document different from the id provided,
    /// the id will be updated too. After the operation succeed there will be no document with the old id.
    /// </remarks>
    /// <param name="options">The request options.</param>
    /// <param name="id">The document id.</param>
    /// <param name="conflictResolveFunc">Func used to resolve conflict if there are documents in DB.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> containing a <see cref="T:System.Cloud.DocumentDb.IDatabaseResponse`1" /> which wraps the updated document.</returns>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    Task<IDatabaseResponse<TDocument>> InsertOrUpdateDocumentAsync(RequestOptions<TDocument> options, string id, Func<TDocument, TDocument> conflictResolveFunc, CancellationToken cancellationToken);

    /// <summary>
    /// Upserts a document provided in <see cref="P:System.Cloud.DocumentDb.RequestOptions`1.Document" />.
    /// </summary>
    /// <remarks>
    /// This method is suitable when existence of a document is unknown, and replace is always suitable.
    /// For conditional replace <see cref="M:System.Cloud.DocumentDb.IDocumentWriter`1.InsertOrUpdateDocumentAsync(System.Cloud.DocumentDb.RequestOptions{`0},System.String,System.Func{`0,`0},System.Threading.CancellationToken)" /> should be used instead.
    /// </remarks>
    /// <param name="options">The request options.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> containing a <see cref="T:System.Cloud.DocumentDb.IDatabaseResponse`1" /> which wraps the updated document.</returns>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    Task<IDatabaseResponse<TDocument>> UpsertDocumentAsync(RequestOptions<TDocument> options, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a document associated with the id.
    /// </summary>
    /// <param name="options">The request options.</param>
    /// <param name="id">The id of the document to be deleted.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> containing a <see cref="T:System.Cloud.DocumentDb.IDatabaseResponse`1" /> which wraps the asynchronous operation result.
    /// Result of the operation is true when deletion succeed, false if failed.</returns>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    Task<IDatabaseResponse<bool>> DeleteDocumentAsync(RequestOptions<TDocument> options, string id, CancellationToken cancellationToken);

    /// <summary>
    /// Transactionally executes a set of operations.
    /// </summary>
    /// <remarks>
    /// Transactional batch describes a group of point operations that
    /// need to either succeed or fail. If all operations, in the order that are described in the transactional batch,
    /// succeed, the transaction is committed. If any operation fails, the entire transaction is rolled back.
    /// </remarks>
    /// <param name="requestOptions">The request options.</param>
    /// <param name="itemsToPerformTransactionalBatch"><see cref="T:System.Collections.Generic.IList`1" /> to perform transaction batch.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> containing a <see cref="T:System.Collections.Generic.IList`1" /> of <see cref="T:System.Cloud.DocumentDb.IDatabaseResponse`1" /> which wraps transaction operation response.</returns>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    Task<IDatabaseResponse<IReadOnlyList<IDatabaseResponse<TDocument>>>> ExecuteTransactionalBatchAsync(RequestOptions<TDocument> requestOptions, IReadOnlyList<BatchItem<TDocument>> itemsToPerformTransactionalBatch, CancellationToken cancellationToken);
}
