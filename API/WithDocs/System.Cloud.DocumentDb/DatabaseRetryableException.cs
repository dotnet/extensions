// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

/// <summary>
/// Exception represent the operation is failed w/ a specific reason and it's eligible to retry in future.
/// </summary>
/// <remarks>
/// Http code 429, 503, 408.
/// Covered codes may vary on specific engine requirements.
/// </remarks>
public class DatabaseRetryableException : DatabaseException
{
    /// <summary>
    /// Gets a value indicate the retry after time.
    /// </summary>
    public TimeSpan RetryAfter { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.DatabaseRetryableException" /> class.
    /// </summary>
    public DatabaseRetryableException();

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.DatabaseRetryableException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public DatabaseRetryableException(string message);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.DatabaseRetryableException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Exception related to the missing data.</param>
    public DatabaseRetryableException(string message, Exception innerException);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.DatabaseRetryableException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Exception related to the missing data.</param>
    /// <param name="statusCode">Exception status code.</param>
    /// <param name="subStatusCode">Exception sub status code.</param>
    /// <param name="retryAfter">Retry after timespan.</param>
    /// <param name="requestInfo">The request.</param>
    public DatabaseRetryableException(string message, Exception innerException, int statusCode, int subStatusCode, TimeSpan? retryAfter, RequestInfo requestInfo);
}
