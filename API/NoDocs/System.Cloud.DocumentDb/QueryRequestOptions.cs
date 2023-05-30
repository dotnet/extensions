// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

public class QueryRequestOptions<TDocument> : RequestOptions<TDocument> where TDocument : notnull
{
    [Range(1, 10000)]
    public int? ResponseContinuationTokenLimitInKb { get; set; }
    public bool? EnableScan { get; set; }
    public bool? EnableLowPrecisionOrderBy { get; set; }
    [Range(1, 1000000)]
    public int? MaxBufferedItemCount { get; set; }
    [Range(1, 1000000)]
    public int? MaxResults { get; set; }
    [Range(1, 1000)]
    public int? MaxConcurrency { get; set; }
    public string? ContinuationToken { get; set; }
    public FetchMode FetchCondition { get; set; }
    public QueryRequestOptions();
}
