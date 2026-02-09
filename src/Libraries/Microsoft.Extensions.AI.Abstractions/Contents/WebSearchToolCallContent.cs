// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a web search tool call invocation by a hosted service.
/// </summary>
/// <remarks>
/// This content type represents when a hosted AI service invokes a web search tool.
/// It is informational only and represents the call itself, not the result.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIWebSearch, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class WebSearchToolCallContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebSearchToolCallContent"/> class.
    /// </summary>
    public WebSearchToolCallContent()
    {
    }

    /// <summary>
    /// Gets or sets the tool call ID.
    /// </summary>
    public string? CallId { get; set; }

    /// <summary>
    /// Gets or sets the search queries issued by the service.
    /// </summary>
    public IList<string>? Queries { get; set; }
}
