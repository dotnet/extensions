// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

/// <summary>
/// The class representing a query with parameters.
/// </summary>
public readonly struct Query
{
    /// <summary>
    /// Gets the query text.
    /// </summary>
    public string QueryText { get; }

    /// <summary>
    /// Gets the query parameters.
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.Query" /> struct.
    /// </summary>
    /// <param name="queryText">The query text.</param>
    /// <param name="parameters">The query parameters.</param>
    public Query(string queryText, IReadOnlyDictionary<string, string> parameters);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.Query" /> struct.
    /// </summary>
    /// <param name="queryText">The query text.</param>
    public Query(string queryText);
}
