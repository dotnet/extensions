// Assembly 'System.Cloud.DocumentDb.Abstractions'

namespace System.Cloud.DocumentDb;

public class DatabaseClientException : DatabaseException
{
    public DatabaseClientException();
    public DatabaseClientException(string message);
    public DatabaseClientException(string message, Exception innerException);
}
