// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.DocumentDb;

/// <summary>
/// An interface for managing a document database.
/// </summary>
/// <remarks>
/// It plays a role of database, table and connection management.
/// Also it allows constructing readers and writers for tables.
/// </remarks>
public interface IDocumentDatabase
{
    /// <summary>
    /// Gets a document reader for a table and a document type provided.
    /// </summary>
    /// <param name="options">The table options.</param>
    /// <returns>The document reader.</returns>
    /// <typeparam name="TDocument">
    /// The document entity type to be used as a table schema.
    /// Operation results of request will be mapped to instance of this type.
    /// </typeparam>
    /// <exception cref="DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    IDocumentReader<TDocument> GetDocumentReader<TDocument>(TableOptions options)
        where TDocument : notnull;

    /// <summary>
    /// Gets a document writer for a table and a document type provided.
    /// </summary>
    /// <param name="options">The table options.</param>
    /// <returns>The document writer.</returns>
    /// <typeparam name="TDocument">
    /// The document entity type to be used as a table schema.
    /// Operation results of request will be mapped to instance of this type.
    /// </typeparam>
    /// <exception cref="DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    IDocumentWriter<TDocument> GetDocumentWriter<TDocument>(TableOptions options)
        where TDocument : notnull;

    /// <summary>
    /// Initializes connections and optionally creates database if not exists.
    /// </summary>
    /// <param name="createIfNotExists">Specifies whether database should be created if not exists.</param>
    /// <param name="cancellationToken">The cancelation token.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    Task ConnectAsync(bool createIfNotExists, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes database this instance is responsible for.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A <see cref="Task"/> containing a <see cref="IDatabaseResponse{T}"/> with
    /// <see langword="true"/> value if successfully deleted and <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    Task<IDatabaseResponse<bool>> DeleteDatabaseAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reads provided table settings.
    /// </summary>
    /// <param name="tableOptions">The table options with <see cref="TableOptions.TableName"/> and region information provided.</param>
    /// <param name="requestOptions">The request options.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing a <see cref="IDatabaseResponse{T}"/> which wraps the table information.</returns>
    /// <exception cref="DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    Task<IDatabaseResponse<TableOptions>> ReadTableSettingsAsync(
        TableOptions tableOptions,
        RequestOptions requestOptions,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates existing table settings.
    /// </summary>
    /// <param name="tableOptions">The table options with <see cref="TableOptions.TableName"/> and region information provided.</param>
    /// <param name="requestOptions">The request options.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>
    /// A <see cref="Task"/> containing a <see cref="IDatabaseResponse{T}"/> which wraps the asynchronous operation result.
    /// The result is <see langword="true"/> when the throughput replaced successfully.
    /// The <see langword="false"/> indicating the operation is pending.
    /// </returns>
    /// <exception cref="DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    /// <seealso cref="Throughput.Value"/>
    Task<IDatabaseResponse<bool>> UpdateTableSettingsAsync(
        TableOptions tableOptions,
        RequestOptions requestOptions,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates table using provided parameters.
    /// </summary>
    /// <param name="tableOptions">The table options.</param>
    /// <param name="requestOptions">The request options.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing a <see cref="IDatabaseResponse{T}"/> which wraps the table information.</returns>
    /// <exception cref="DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    Task<IDatabaseResponse<TableOptions>> CreateTableAsync(
        TableOptions tableOptions,
        RequestOptions requestOptions,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes table using provided parameters.
    /// </summary>
    /// <param name="tableOptions">The table options with <see cref="TableOptions.TableName"/> and region information provided.</param>
    /// <param name="requestOptions">The request options.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>
    /// A <see cref="Task"/> containing a <see cref="IDatabaseResponse{T}"/> with
    /// <see langword="true"/> value if table successfully deleted and <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    Task<IDatabaseResponse<TableOptions>> DeleteTableAsync(
        TableOptions tableOptions,
        RequestOptions requestOptions,
        CancellationToken cancellationToken);
}

/// <summary>
/// An interface for injecting <see cref="IDocumentDatabase"/> to a specific context.
/// </summary>
/// <typeparam name="TContext">The context type, indicating injection preferences.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S4023:Interfaces should not be empty",
    Justification = "It is designed for adding an indicator type only, not members.")]
public interface IDocumentDatabase<TContext> : IDocumentDatabase
    where TContext : class
{
}
