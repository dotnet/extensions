// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

/// <summary>
/// Describes the request information.
/// </summary>
public readonly struct RequestInfo
{
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

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.RequestInfo" /> struct.
    /// </summary>
    /// <param name="region">The request region.</param>
    /// <param name="tableName">The request table name.</param>
    /// <param name="cost">The request cost.</param>
    /// <param name="endpoint">The endpoint used for request.</param>
    public RequestInfo(string? region = null, string? tableName = null, double? cost = null, Uri? endpoint = null);
}
