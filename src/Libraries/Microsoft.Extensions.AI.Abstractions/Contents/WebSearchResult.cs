// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents an individual web search result.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIWebSearch, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class WebSearchResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebSearchResult"/> class.
    /// </summary>
    public WebSearchResult()
    {
    }

    /// <summary>
    /// Gets or sets the title of the web page.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the URL of the web page.
    /// </summary>
    public Uri? Url { get; set; }

    /// <summary>
    /// Gets or sets a text snippet or excerpt from the web page.
    /// </summary>
    public string? Snippet { get; set; }

    /// <summary>Gets or sets additional properties for the result.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
