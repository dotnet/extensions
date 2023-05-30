// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

/// <summary>
/// Defines parameters to be used by store engine.
/// </summary>
/// <remarks>
/// Not all parameters supported by all APIs and engines.
/// Unsupported parameters are ignored.
/// </remarks>
/// <typeparam name="TDocument">
/// The document entity type to be used as a table schema.
/// Operation results from database will be mapped to instance of this type.
/// </typeparam>
public class RequestOptions<TDocument> : RequestOptions where TDocument : notnull
{
    /// <summary>
    /// Gets or sets the document value.
    /// </summary>
    public TDocument? Document { get; set; }

    public RequestOptions();
}
