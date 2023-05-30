// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Net;
using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

public class DatabaseException : Exception
{
    public int StatusCode { get; }
    public HttpStatusCode HttpStatusCode { get; }
    public int SubStatusCode { get; }
    public RequestInfo RequestInfo { get; }
    public DatabaseException();
    public DatabaseException(string message);
    public DatabaseException(string message, Exception innerException);
    public DatabaseException(string message, int statusCode, int subStatusCode, RequestInfo requestInfo);
    public DatabaseException(string message, Exception innerException, int statusCode, int subStatusCode, RequestInfo requestInfo);
}
