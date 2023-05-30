// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Net;
using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

/// <summary>
/// Base type for exceptions thrown by storage adapter.
/// </summary>
public class DatabaseException : Exception
{
    /// <summary>
    /// Gets the status code indicating the exception root cause.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets the status code indicating the exception root cause.
    /// </summary>
    public HttpStatusCode HttpStatusCode { get; }

    /// <summary>
    /// Gets the status code indicating the exception root cause.
    /// </summary>
    public int SubStatusCode { get; }

    /// <summary>
    /// Gets the request information.
    /// </summary>
    public RequestInfo RequestInfo { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.DatabaseException" /> class.
    /// </summary>
    public DatabaseException();

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.DatabaseException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public DatabaseException(string message);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.DatabaseException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception causing this exception.</param>
    public DatabaseException(string message, Exception innerException);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.DatabaseException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="statusCode">Exception status code.</param>
    /// <param name="subStatusCode">Exception sub status code.</param>
    /// <param name="requestInfo">The request.</param>
    public DatabaseException(string message, int statusCode, int subStatusCode, RequestInfo requestInfo);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.DatabaseException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception causing this exception.</param>
    /// <param name="statusCode">Exception status code.</param>
    /// <param name="subStatusCode">Exception sub status code.</param>
    /// <param name="requestInfo">The request.</param>
    public DatabaseException(string message, Exception innerException, int statusCode, int subStatusCode, RequestInfo requestInfo);
}
