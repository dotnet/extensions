// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the result of a hosted MCP server tool call.
/// </summary>
public class HostedMcpServerToolResultContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostedMcpServerToolResultContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    public HostedMcpServerToolResultContent(string callId)
    {
        CallId = Throw.IfNullOrWhitespace(callId);
    }

    /// <summary>
    /// Gets the tool call ID.
    /// </summary>
    public string CallId { get; }

    /// <summary>
    /// Gets or sets the output of the tool call.
    /// </summary>
    public IList<AIContent>? Output { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the result was an error.
    /// </summary>
    public bool IsError { get; set; }
}
