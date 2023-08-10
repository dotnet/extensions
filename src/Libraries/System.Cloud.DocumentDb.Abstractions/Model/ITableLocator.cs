// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Cloud.DocumentDb;

/// <summary>
/// Provides a way to adjust table parameters based on a specific document.
/// </summary>
/// <remarks>
/// This interface may be useful if table settings such as name or region differs on data provided.
/// For example, a specific tenant's data should be isolated from other data or encrypted differently or live in other region.
/// This can be done on the user side, however if the user has a lot places to access, it is troublesome and error prone to be done in all places.
/// Instead, you can delegate the logic to an adapter to be applied every time a client makes a request.
/// </remarks>
public interface ITableLocator
{
    /// <summary>
    /// Provides a way to adjust table and request parameters for a specified request.
    /// </summary>
    /// <param name="options">The original table options.</param>
    /// <param name="request">The target request.</param>
    /// <remarks>
    /// This method will be called only in cases <see cref="TableOptions.IsLocatorRequired"/> is set to true.
    /// The input table options should not be modified; those are original options used to initialize reader / writer.
    /// The method can adjust table name, region in request, or other options specific to the provided document or request.
    /// For example:
    /// - A specific region might have a different table name, throughput requirements, or TTL.
    /// - A specific document might have a region or table requirement different from original.
    /// Notes:
    /// - The <paramref name="request"/> object is not shared between calls; it can be modified by the method directly.
    /// - The <paramref name="request"/> is the same provided for the API call.
    /// If a document is needed to implement locate logic, use <see cref="RequestOptions{TDocument}"/> for requests.
    /// </remarks>
    /// <returns>A new table options, or the input options if no adjustments are needed.</returns>
    TableInfo? LocateTable(in TableInfo options, RequestOptions request);
}
