// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

public class DatabaseRetryableException : DatabaseException
{
    public TimeSpan RetryAfter { get; }
    public DatabaseRetryableException();
    public DatabaseRetryableException(string message);
    public DatabaseRetryableException(string message, Exception innerException);
    public DatabaseRetryableException(string message, Exception innerException, int statusCode, int subStatusCode, TimeSpan? retryAfter, RequestInfo requestInfo);
}
