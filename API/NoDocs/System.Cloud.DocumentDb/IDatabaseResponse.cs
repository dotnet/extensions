// Assembly 'System.Cloud.DocumentDb.Abstractions'

namespace System.Cloud.DocumentDb;

public interface IDatabaseResponse
{
    int Status { get; }
    RequestInfo RequestInfo { get; }
    string? ItemVersion { get; }
    bool Succeeded { get; }
    string? ContinuationToken { get; }
    int ItemCount { get; }
}
