// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the result of a MCP server tool call.
/// </summary>
/// <remarks>
/// This content type is used to represent the result of an invocation of an MCP server tool by a hosted service.
/// It is informational only.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIMcpServers, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class McpServerToolResultContent : FunctionResultContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpServerToolResultContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    /// <exception cref="ArgumentNullException"><paramref name="callId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="callId"/> is empty or composed entirely of whitespace.</exception>
    [JsonConstructor]
    public McpServerToolResultContent(string callId)
        : base(Throw.IfNullOrWhitespace(callId), result: null)
    {
    }
}
