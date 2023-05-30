// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.DocumentDb;

/// <summary>
/// An interface to clients for all document oriented (or NoSQL) databases for document read operations.
/// https://en.wikipedia.org/wiki/Document-oriented_database.
/// </summary>
/// <typeparam name="TDocument">
/// The document entity type to be used as a table schema.
/// Request results will be mapped to instance of this type.
/// </typeparam>
public interface IDocumentReader<TDocument> where TDocument : notnull
{
    /// <summary>
    /// Reads a document.
    /// </summary>
    /// <param name="requestOptions">The request options.</param>
    /// <param name="id">The document id requested to read.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> containing a <see cref="T:System.Cloud.DocumentDb.IDatabaseResponse`1" /> which wraps the result document.</returns>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    Task<IDatabaseResponse<TDocument>> ReadDocumentAsync(RequestOptions<TDocument> requestOptions, string id, CancellationToken cancellationToken);

    /// <summary>
    /// Fetches a collection of documents.
    /// </summary>
    /// <param name="options">The request options.</param>
    /// <param name="condition">The fetch condition function.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> containing a <see cref="T:System.Cloud.DocumentDb.IDatabaseResponse`1" /> which wraps enumerable of fetched documents.</returns>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    /// <typeparam name="TOutputDocument">The type used to map results from <typeparamref name="TDocument" /> after condition is applied.</typeparam>
    Task<IDatabaseResponse<IReadOnlyList<TOutputDocument>>> FetchDocumentsAsync<TOutputDocument>(QueryRequestOptions<TDocument> options, Func<IQueryable<TDocument>, IQueryable<TOutputDocument>>? condition, CancellationToken cancellationToken) where TOutputDocument : notnull;

    /// <summary>
    /// Fetches a collection of documents using a custom query provided.
    /// </summary>
    /// <param name="options">The query request options.</param>
    /// <param name="query">The query to fetch items.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> containing a <see cref="T:System.Cloud.DocumentDb.IDatabaseResponse`1" /> which wraps enumerable of fetched documents.</returns>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    Task<IDatabaseResponse<IReadOnlyList<TDocument>>> QueryDocumentsAsync(QueryRequestOptions<TDocument> options, Query query, CancellationToken cancellationToken);

    /// <summary>
    /// Counts documents which satisfy a query conditions in a table.
    /// </summary>
    /// <param name="options">The query request options. </param>
    /// <param name="condition">The condition function.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> containing a count of documents.</returns>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="T:System.Cloud.DocumentDb.DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    Task<IDatabaseResponse<int>> CountDocumentsAsync(QueryRequestOptions<TDocument> options, Func<IQueryable<TDocument>, IQueryable<TDocument>>? condition, CancellationToken cancellationToken);
}
