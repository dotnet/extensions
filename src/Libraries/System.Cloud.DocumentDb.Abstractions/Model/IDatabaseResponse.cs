// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace System.Cloud.DocumentDb;

/// <summary>
/// The result interface for document storage responses.
/// </summary>
public interface IDatabaseResponse
{
    /// <summary>
    /// Gets the response status code.
    /// </summary>
    /// <remarks>
    /// For databases using HTTP protocol the value would be the <see cref="HttpStatusCode"/> of response.
    /// </remarks>
    int Status { get; }

    /// <summary>
    /// Gets the request information.
    /// </summary>
    RequestInfo RequestInfo { get; }

    /// <summary>
    /// Gets the item version string.
    /// </summary>
    /// <remarks>
    /// For HTTP based protocols it could be based on ETag property.
    /// If provided this version can be used in update APIs for consistency control.
    /// </remarks>
    string? ItemVersion { get; }

    /// <summary>
    /// Gets a value indicating whether an operation succeeded.
    /// </summary>
    bool Succeeded { get; }

    /// <summary>
    /// Gets a value indicate the start point of next batch read.
    /// </summary>
    string? ContinuationToken { get; }

    /// <summary>
    /// Gets count of items in result.
    /// </summary>
    /// <remarks>
    /// If <see cref="IDatabaseResponse{T}.Item"/> hold a list, this property returns the number of items in the list. Otherwise it returns 1.
    /// This should be used when type is unknown in a context,
    /// and count only needed for telemetry or logging.
    /// </remarks>
    int ItemCount { get; }
}

/// <summary>
/// The result interface including item for document storage responses.
/// </summary>
/// <typeparam name="T">The type of the item the response contains.</typeparam>
public interface IDatabaseResponse<out T> : IDatabaseResponse
    where T : notnull
{
    /// <summary>
    /// Gets response item.
    /// </summary>
    T? Item { get; }
}
