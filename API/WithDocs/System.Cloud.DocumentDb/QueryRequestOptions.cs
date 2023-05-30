// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

/// <summary>
/// Defines parameters to be used by store engine for queries.
/// </summary>
/// <typeparam name="TDocument">
/// The document entity type to be used as a table schema.
/// Request results will be mapped to instance of this type.
/// </typeparam>
public class QueryRequestOptions<TDocument> : RequestOptions<TDocument> where TDocument : notnull
{
    /// <summary>
    /// Gets or sets the continuation token size limit.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// </remarks>
    [Range(1, 10000)]
    public int? ResponseContinuationTokenLimitInKb { get; set; }

    /// <summary>
    /// Gets or sets the option to enable scans on the queries which couldn't be served
    /// as indexing was opted out on the requested paths.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// Set true to enable and false to disable scan in query.
    /// When set to null, client will use database configured or default option.
    /// If operation does not support the option, this parameter will be ignored.
    /// </remarks>
    public bool? EnableScan { get; set; }

    /// <summary>
    /// Gets or sets the option to enable low precision order by.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// </remarks>
    public bool? EnableLowPrecisionOrderBy { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items that can be buffered client side during parallel query execution.
    /// </summary>
    /// <remarks>
    /// The default is <see langword="null" />.
    /// A positive property value limits the number of buffered items to the set value.
    /// If this is set to <see langword="null" />, the system automatically decides the number of items to buffer.
    /// This is only suggestive and cannot be abided by in certain cases.
    /// </remarks>
    [Range(1, 1000000)]
    public int? MaxBufferedItemCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items to be returned.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// </remarks>
    [Range(1, 1000000)]
    public int? MaxResults { get; set; }

    /// <summary>
    /// Gets or sets the number of concurrent operations run client side during parallel query execution.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// A positive property value limits the number of concurrent operations to the set value.
    /// If this is set to <see langword="null" />, the system automatically decides the number of concurrent operations to run.
    /// </remarks>
    [Range(1, 1000)]
    public int? MaxConcurrency { get; set; }

    /// <summary>
    /// Gets or sets continuation token to continue reading from a breakpoint.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" /> and reading would start from the begin.
    /// </remarks>
    public string? ContinuationToken { get; set; }

    /// <summary>
    /// Gets or sets the fetch condition.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="F:System.Cloud.DocumentDb.FetchMode.FetchAll" />.
    /// This value is indicate the fetch condition of query.
    /// </remarks>
    public FetchMode FetchCondition { get; set; }

    public QueryRequestOptions();
}
