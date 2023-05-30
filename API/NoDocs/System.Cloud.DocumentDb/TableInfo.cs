// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

public readonly struct TableInfo
{
    public string TableName { get; }
    public TimeSpan TimeToLive { get; }
    public string? PartitionIdPath { get; }
    public bool IsRegional { get; }
    public Throughput Throughput { get; }
    public bool IsLocatorRequired { get; }
    public TableInfo(TableOptions options);
    public TableInfo(in TableInfo info, string? tableNameOverride = null, bool? isRegionalOverride = null);
}
