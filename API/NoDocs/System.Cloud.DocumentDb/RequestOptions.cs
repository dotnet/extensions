// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

public class RequestOptions
{
    public bool ContentResponseOnWrite { get; set; }
    public IReadOnlyList<object?>? PartitionKey { get; set; }
    public ConsistencyLevel? ConsistencyLevel { get; set; }
    public string? SessionToken { get; set; }
    public string? ItemVersion { get; set; }
    public string? Region { get; set; }
    public RequestOptions();
}
