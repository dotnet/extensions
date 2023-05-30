// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

/// <summary>
/// Defines parameters to be used by store engine.
/// </summary>
/// <remarks>
/// Not all parameters supported by all APIs and engines.
/// Unsupported parameters are ignored.
/// </remarks>
public class RequestOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether written object should be returned back after write operations.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false" />.
    /// </value>
    /// <remarks>
    /// Indicates whether written object should be returned back after write operations like Create, Upsert, Patch, and Replace.
    /// Setting the option to false causes the response to have a <see langword="null" /> item.
    /// This reduces networking and CPU load by not sending the resource back over the network and serializing it on the client.
    /// </remarks>
    public bool ContentResponseOnWrite { get; set; }

    /// <summary>
    /// Gets or sets the partition key elements for the current request.
    /// </summary>
    /// <value>
    /// The default is <see langword="null" />.
    /// </value>
    public IReadOnlyList<object?>? PartitionKey { get; set; }

    /// <summary>
    /// Gets or sets the consistency level required for the request.
    /// </summary>
    /// <value>
    /// The default is <see langword="null" />.
    /// </value>
    public ConsistencyLevel? ConsistencyLevel { get; set; }

    /// <summary>
    /// Gets or sets the token for use with session consistency.
    /// </summary>
    /// <value>
    /// The default is <see langword="null" />.
    /// </value>
    public string? SessionToken { get; set; }

    /// <summary>
    /// Gets or sets the item version parameter to control item version for concurrent modifications.
    /// </summary>
    /// <value>
    /// The default is <see langword="null" />.
    /// </value>
    /// <remarks>
    /// For HTTP based protocols, the item version could be based on ETag property.
    /// It can be obtained from <see cref="P:System.Cloud.DocumentDb.IDatabaseResponse.ItemVersion" />
    /// by performing an operation that provides the item as the result.
    /// </remarks>
    public string? ItemVersion { get; set; }

    /// <summary>
    /// Gets or sets the region ID.
    /// </summary>
    /// <value>
    /// The default is <see langword="null" />.
    /// </value>
    /// <remarks>
    /// If the region is not set, the request will work with global database.
    /// Otherwise it should operate with database of a specified region.
    /// </remarks>
    public string? Region { get; set; }

    public RequestOptions();
}
