// Assembly 'System.Cloud.DocumentDb.Abstractions'

namespace System.Cloud.DocumentDb;

public interface IDatabaseResponse<out T> : IDatabaseResponse where T : notnull
{
    T? Item { get; }
}
