// Assembly 'System.Cloud.DocumentDb.Abstractions'

namespace System.Cloud.DocumentDb;

public class DatabaseServerException : DatabaseException
{
    public DatabaseServerException();
    public DatabaseServerException(string message);
    public DatabaseServerException(string message, Exception innerException);
    public DatabaseServerException(string message, Exception innerException, int statusCode, int subStatusCode, RequestInfo requestInfo);
    public DatabaseServerException(string message, int statusCode, int subStatusCode, RequestInfo requestInfo);
}
