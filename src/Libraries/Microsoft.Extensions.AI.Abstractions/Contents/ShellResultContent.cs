// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the result of a shell tool invocation by a hosted service.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIShell, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ShellResultContent : ToolResultContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShellResultContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    public ShellResultContent(string callId)
        : base(callId)
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
