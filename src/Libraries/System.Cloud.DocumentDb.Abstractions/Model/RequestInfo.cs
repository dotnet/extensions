// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Cloud.DocumentDb;

/// <summary>
/// Describes the request information.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types",
    Justification = "Not to be compared or used as a key of key value maps.")]
public readonly struct RequestInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestInfo"/> struct.
    /// </summary>
    /// <param name="region">The request region.</param>
    /// <param name="tableName">The request table name.</param>
    /// <param name="cost">The request cost.</param>
    /// <param name="endpoint">The endpoint used for request.</param>
    public RequestInfo(string? region = null, string? tableName = null, double? cost = null, Uri? endpoint = null)
    {
        Region = region;
        TableName = tableName;
        Cost = cost;
        Endpoint = endpoint;
    }

    /// <summary>
    /// Gets target region, if available.
    /// </summary>
    public string? Region { get; }

    /// <summary>
    /// Gets target table name, if available.
    /// </summary>
    public string? TableName { get; }

    /// <summary>
    /// Gets the cost of the request in database defined units if available.
    /// </summary>
    public double? Cost { get; }

    /// <summary>
    /// Gets the endpoint used for request.
    /// </summary>
    public Uri? Endpoint { get; }
}
