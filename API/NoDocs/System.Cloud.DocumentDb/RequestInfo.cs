// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

public readonly struct RequestInfo
{
    public string? Region { get; }
    public string? TableName { get; }
    public double? Cost { get; }
    public Uri? Endpoint { get; }
    public RequestInfo(string? region = null, string? tableName = null, double? cost = null, Uri? endpoint = null);
}
