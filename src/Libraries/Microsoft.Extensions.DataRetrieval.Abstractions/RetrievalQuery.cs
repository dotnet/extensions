// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.DataRetrieval;

/// <summary>
/// Represents a retrieval query with optional expanded variants and metadata.
/// </summary>
public sealed class RetrievalQuery
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetrievalQuery"/> class.
    /// </summary>
    /// <param name="text">The original query text.</param>
    public RetrievalQuery(string text)
    {
        Text = text;
        Variants = [text];
    }

    /// <summary>
    /// Gets the original query text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets or sets the query variants to search with.
    /// </summary>
    /// <remarks>
    /// Pre-query processors may expand a single query into multiple variants
    /// (e.g., multi-query expansion, HyDE). Each variant is searched independently
    /// and results are merged using Reciprocal Rank Fusion.
    /// </remarks>
    public IList<string> Variants { get; set; }

    /// <summary>
    /// Gets the metadata associated with this query.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; } = new Dictionary<string, object?>();
}
