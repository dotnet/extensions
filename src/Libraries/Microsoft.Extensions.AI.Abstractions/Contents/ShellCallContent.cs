// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a shell tool call invocation by a hosted service.
/// </summary>
/// <remarks>
/// This content type is produced by <see cref="IChatClient"/> implementations that have native shell tool support.
/// It is informational only and represents the call itself, not the result.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIShell, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ShellCallContent : ToolCallContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShellCallContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    public ShellCallContent(string callId)
        : base(callId)
    {
    }

    /// <summary>
    /// Gets or sets the list of commands to execute.
    /// </summary>
    public IList<string>? Commands { get; set; }

    /// <summary>
    /// Gets or sets the timeout for the shell command execution.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum output length in characters.
    /// </summary>
    public int? MaxOutputLength { get; set; }

    /// <summary>
    /// Gets or sets the status of the shell call.
    /// </summary>
    public string? Status { get; set; }
}
