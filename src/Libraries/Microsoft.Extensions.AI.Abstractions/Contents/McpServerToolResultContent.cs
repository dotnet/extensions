// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the result of a MCP server tool call.
/// </summary>
/// <remarks>
/// This content type is used to represent the result of an invocation of an MCP server tool by a hosted service.
/// It is informational only.
/// </remarks>
[Experimental("MEAI001")]
public sealed class McpServerToolResultContent : ServiceActionContent
{
    /// <inheritdoc/>
    public McpServerToolResultContent(string id)
        : base(id)
    {
    }

    /// <summary>
    /// Gets or sets the output of the tool call.
    /// </summary>
    public IList<AIContent>? Output { get; set; }
}
