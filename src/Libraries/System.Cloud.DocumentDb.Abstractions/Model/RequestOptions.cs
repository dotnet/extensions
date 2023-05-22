// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Cloud.DocumentDb;

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Defines parameters to be used by store engine.
/// </summary>
/// <remarks>
/// Not all parameters supported by all APIs and engines.
/// Not supported parameters will be ignored.
/// </remarks>
/// <typeparam name="TDocument">
/// The document entity type to be used as a table schema.
/// Operation results from database will be mapped to instance of this type.
/// </typeparam>
public class RequestOptions<TDocument> : RequestOptions
    where TDocument : notnull
{
    /// <summary>
    /// Gets or sets the document value.
    /// </summary>
    public TDocument? Document { get; set; }
}

/// <summary>
/// Defines parameters to be used by store engine.
/// </summary>
/// <remarks>
/// Not all parameters supported by all APIs and engines.
/// Not supported parameters will be ignored.
/// </remarks>
public class RequestOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether written object should be returned back after write operations.
    /// </summary>
    /// <remarks>
    /// Indicating whether written object should be returned back after write operations like Create, Upsert, Patch and Replace.
    /// Setting the option to false will cause the response to have a null item.
    /// This reduces networking and CPU load by not sending the resource back over the network and serializing it on the client.
    /// Default is <see langword="false" />.
    /// </remarks>
    public bool ContentResponseOnWrite { get; set; }

    /// <summary>
    /// Gets or sets the partition key elements for the current request.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// </remarks>
    public IReadOnlyList<object?>? PartitionKey { get; set; }

    /// <summary>
    /// Gets or sets the consistency level required for the request.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// </remarks>
    public ConsistencyLevel? ConsistencyLevel { get; set; }

    /// <summary>
    /// Gets or sets the token for use with session consistency.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// </remarks>
    public string? SessionToken { get; set; }

    /// <summary>
    /// Gets or sets the item version parameter to control item version for concurrent modifications.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// For HTTP based protocols it could be based on ETag property.
    /// It can be obtained from <see cref="IDatabaseResponse.ItemVersion"/>
    /// by doing operation providing item as result.
    /// </remarks>
    public string? ItemVersion { get; set; }

    /// <summary>
    /// Gets or sets the region id.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// If the region is not set, request will work with global database.
    /// Otherwise it should operate with database of specified region.
    /// </remarks>
    public string? Region { get; set; }
}
