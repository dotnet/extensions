// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Shared.Collections;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.DocumentDb;

/// <summary>
/// The class representing a query with parameters.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types",
    Justification = "Not to be used as a key in key value structs.")]
public readonly struct Query
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Query"/> struct.
    /// </summary>
    /// <param name="queryText">The query text.</param>
    /// <param name="parameters">The query parameters.</param>
    public Query(string queryText, IReadOnlyDictionary<string, string> parameters)
    {
        QueryText = Throw.IfNull(queryText);
        Parameters = Throw.IfNull(parameters);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Query"/> struct.
    /// </summary>
    /// <param name="queryText">The query text.</param>
    public Query(string queryText)
        : this(queryText, Empty.ReadOnlyDictionary<string, string>())
    {
    }

    /// <summary>
    /// Gets the query text.
    /// </summary>
    public string QueryText { get; }

    /// <summary>
    /// Gets the query parameters.
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; }
}
