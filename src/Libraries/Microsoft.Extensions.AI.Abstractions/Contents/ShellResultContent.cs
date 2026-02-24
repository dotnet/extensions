// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the result of a shell tool invocation.
/// </summary>
/// <remarks>
/// This content type extends <see cref="FunctionResultContent"/> with structured shell output data.
/// It can be produced by <see cref="ShellTool"/> subclasses for local execution or by
/// <see cref="IChatClient"/> implementations for hosted shell execution results.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIShell, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ShellResultContent : FunctionResultContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShellResultContent"/> class.
    /// </summary>
    /// <param name="callId">The function call ID for which this is the result.</param>
    /// <param name="result">The result of the shell tool invocation.</param>
    [JsonConstructor]
    public ShellResultContent(string callId, object? result = null)
        : base(Throw.IfNull(callId), result)
    {
    }

    /// <summary>
    /// Gets or sets the output of the shell command execution.
    /// </summary>
    public IList<ShellCommandOutput>? Output { get; set; }

    /// <summary>
    /// Gets or sets the maximum output length in characters.
    /// </summary>
    public int? MaxOutputLength { get; set; }
}
