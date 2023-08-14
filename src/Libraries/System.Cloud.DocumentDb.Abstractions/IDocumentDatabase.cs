// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.DocumentDb;

/// <summary>
/// An interface for managing a document database.
/// </summary>
/// <remarks>
/// This interface helps with database, table, and connection management.
/// It also allows constructing readers and writers for tables.
/// </remarks>
public interface IDocumentDatabase
{
    /// <summary>
    /// Gets a document reader for the specified table and document type.
    /// </summary>
    /// <param name="options">The table options.</param>
    /// <returns>The document reader.</returns>
    /// <typeparam name="TDocument">
    /// The document entity type to be used as a table schema.
    /// The results of the request are mapped to an instance of this type.
    /// </typeparam>
    /// <exception cref="DatabaseClientException">An error occurred on the client side,
    /// for example, on a bad request, permissions error, or client timeout.</exception>
    IDocumentReader<TDocument> GetDocumentReader<TDocument>(TableOptions options)
        where TDocument : notnull;

    /// <summary>
    /// Gets a document writer for the specified table and document type.
    /// </summary>
    /// <param name="options">The table options.</param>
    /// <returns>The document writer.</returns>
    /// <typeparam name="TDocument">
    /// The document entity type to be used as a table schema.
    /// The results of the request are mapped to an instance of this type.
    /// </typeparam>
    /// <exception cref="DatabaseClientException">An error occurred on the client side,
    /// for example, on a bad request, permissions error, or client timeout.</exception>
    IDocumentWriter<TDocument> GetDocumentWriter<TDocument>(TableOptions options)
        where TDocument : notnull;

    /// <summary>
    /// Initializes connections and optionally creates the database if it doesn't exist.
    /// </summary>
    /// <param name="createIfNotExists"><see langword="true" /> to create the database if it doesn't exist; otherwise, <see langword="false" /> .</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    Task ConnectAsync(bool createIfNotExists, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the database this instance is responsible for.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A <see cref="Task"/> containing a <see cref="IDatabaseResponse{T}"/> with
    /// <see langword="true"/> value if successfully deleted and <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="DatabaseClientException">An error occurred on the client side,
    /// for example, on a bad request, permissions error, or client timeout.</exception>
    /// <exception cref="DatabaseServerException">An error occurred on the database server side,
    /// including an internal server error.</exception>
    /// <exception cref="DatabaseRetryableException">The request failed but can be retried.
    /// This includes throttling and when the server is unavailable. </exception>
    /// <exception cref="DatabaseException">A general failure occurred.</exception>
    Task<IDatabaseResponse<bool>> DeleteDatabaseAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reads the provided table settings.
    /// </summary>
    /// <param name="tableOptions">The table options with <see cref="TableOptions.TableName"/> and region information provided.</param>
    /// <param name="requestOptions">The request options.</param>
    /// <param name="cancellationToken">The token represents the request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing a <see cref="IDatabaseResponse{T}"/> that wraps the table information.</returns>
    /// <exception cref="DatabaseClientException">An error occurred on the client side,
    /// for example, on a bad request, permissions error, or client timeout.</exception>
    /// <exception cref="DatabaseServerException">An error occurred on the database server side,
    /// including internal server error.</exception>
    /// <exception cref="DatabaseRetryableException">The request failed but can be retried.
    /// This includes throttling and when the server is unavailable. </exception>
    /// <exception cref="DatabaseException">A general failure occurred.</exception>
    Task<IDatabaseResponse<TableOptions>> ReadTableSettingsAsync(
        TableOptions tableOptions,
        RequestOptions requestOptions,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the existing table settings.
    /// </summary>
    /// <param name="tableOptions">The table options with <see cref="TableOptions.TableName"/> and region information provided.</param>
    /// <param name="requestOptions">The request options.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>
    /// A <see cref="Task"/> containing a <see cref="IDatabaseResponse{T}"/> that wraps the asynchronous operation result.
    /// The result is <see langword="true"/> when the throughput replaced successfully.
    /// The <see langword="false"/> indicating the operation is pending.
    /// </returns>
    /// <exception cref="DatabaseClientException">An error occurred on the client side,
    /// for example, on a bad request, permissions error, or client timeout.</exception>
    /// <exception cref="DatabaseServerException">An error occurred on the database server side,
    /// including internal server error.</exception>
    /// <exception cref="DatabaseRetryableException">The request failed but can be retried.
    /// This includes throttling and when the server is unavailable. </exception>
    /// <exception cref="DatabaseException">A general failure occurred.</exception>
    /// <seealso cref="Throughput.Value"/>
    Task<IDatabaseResponse<bool>> UpdateTableSettingsAsync(
        TableOptions tableOptions,
        RequestOptions requestOptions,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a table using provided parameters.
    /// </summary>
    /// <param name="tableOptions">The table options.</param>
    /// <param name="requestOptions">The request options.</param>
    /// <param name="cancellationToken">The token represents request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing a <see cref="IDatabaseResponse{T}"/> that wraps the table information.</returns>
    /// <exception cref="DatabaseClientException">An error occurred on the client side,
    /// for example, on a bad request, permissions error, or client timeout.</exception>
    /// <exception cref="DatabaseServerException">An error occurred on the database server side,
    /// including internal server error.</exception>
    /// <exception cref="DatabaseRetryableException">The request failed but can be retried.
    /// This includes throttling and when the server is unavailable. </exception>
    /// <exception cref="DatabaseException">A general failure occurred.</exception>
    Task<IDatabaseResponse<TableOptions>> CreateTableAsync(
        TableOptions tableOptions,
        RequestOptions requestOptions,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes table using provided parameters.
    /// </summary>
    /// <param name="tableOptions">The table options with <see cref="TableOptions.TableName"/> and region information provided.</param>
    /// <param name="requestOptions">The request options.</param>
    /// <param name="cancellationToken">The token that represents request cancellation.</param>
    /// <returns>
    /// A <see cref="Task"/> containing a <see cref="IDatabaseResponse{T}"/> with
    /// <see langword="true"/> value if table successfully deleted and <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="DatabaseClientException">An error occurred on the client side,
    /// for example, on a bad request, permissions error, or client timeout.</exception>
    /// <exception cref="DatabaseServerException">An error occurred on the database server side,
    /// including internal server error.</exception>
    /// <exception cref="DatabaseRetryableException">The request failed but can be retried.
    /// This includes throttling and when the server is unavailable. </exception>
    /// <exception cref="DatabaseException">A general failure occurred.</exception>
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
