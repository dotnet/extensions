// Assembly 'System.Cloud.DocumentDb.Abstractions'

namespace System.Cloud.DocumentDb;

/// <summary>
/// The interface provides user a way to adjust table parameters based on a specific document.
/// </summary>
/// <remarks>
/// This may be useful if table settings such as a name or a region differs on a data provided.
/// For example specific tenants data should be isolated from other, or encrypted differently or live in other region.
/// This can be done on user side, however if user has a lot places to access, it is troublesome and error prone to be done in all places.
/// Instead a customer can delegate the logic to adapter, to be applied every time a client requested.
/// </remarks>
public interface ITableLocator
{
    /// <summary>
    /// Provides user a way to adjust table and request parameters for specific request.
    /// </summary>
    /// <param name="options">The original table options.</param>
    /// <param name="request">The target request.</param>
    /// <remarks>
    /// This method will be called only in cases <see cref="P:System.Cloud.DocumentDb.TableOptions.IsLocatorRequired" /> is set to true.
    /// The input table options should not be modified, those are original options used to initialize reader / writer.
    /// The method can adjust table name, region in request or other options specific to the provided document and/or request.
    /// e.g.
    /// - specific region may have a different table name, throughput requirements, TTL, etc.
    /// - specific document may have a region or table requirement different from original.
    /// Notes:
    /// - The <paramref name="request" /> object is not shared between calls, it can be modified by the method directly.
    /// - The <paramref name="request" /> is the same provided for the API call.
    /// If document is needed to implement locate logic please use <see cref="T:System.Cloud.DocumentDb.RequestOptions`1" /> for requests.
    /// </remarks>
    /// <returns>A new table options, or same if no adjustments needed.</returns>
    TableInfo? LocateTable(in TableInfo options, RequestOptions request);
}
