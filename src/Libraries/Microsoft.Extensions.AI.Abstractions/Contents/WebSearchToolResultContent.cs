// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the result of a web search tool invocation by a hosted service.
/// </summary>
/// <remarks>
/// This content type represents the results found by a hosted AI service's web search tool.
/// The results contain a list of <see cref="WebSearchResult"/> items, each describing a web page
/// found during the search.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIWebSearch, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class WebSearchToolResultContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebSearchToolResultContent"/> class.
    /// </summary>
    public WebSearchToolResultContent()
    {
    }

    /// <summary>
    /// Gets or sets the tool call ID that this result corresponds to.
    /// </summary>
    public string? CallId { get; set; }

    /// <summary>
    /// Gets or sets the web search results.
    /// </summary>
    /// <remarks>
    /// Each item represents a web page found during the search.
    /// </remarks>
    public IList<WebSearchResult>? Results { get; set; }
}
