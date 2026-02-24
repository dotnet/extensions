// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a shell tool call request from a service.
/// </summary>
/// <remarks>
/// <para>
/// This content type is produced by <see cref="IChatClient"/> implementations that have native shell tool support.
/// It extends <see cref="FunctionCallContent"/> with shell-specific properties such as the list of commands,
/// timeout, and output length constraints.
/// </para>
/// <para>
/// For <see cref="IChatClient"/> implementations without native shell support, the standard
/// <see cref="FunctionCallContent"/> is used instead.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIShell, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ShellCallContent : FunctionCallContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShellCallContent"/> class.
    /// </summary>
    /// <param name="callId">The function call ID.</param>
    /// <param name="name">The tool name.</param>
    /// <param name="arguments">The function arguments.</param>
    [JsonConstructor]
    public ShellCallContent(string callId, string name, IDictionary<string, object?>? arguments = null)
        : base(callId, Throw.IfNull(name), arguments)
    {
    }

    /// <summary>
    /// Gets or sets the list of commands to execute.
    /// </summary>
    public IList<string>? Commands { get; set; }

    /// <summary>
    /// Gets or sets the timeout in milliseconds for the shell command execution.
    /// </summary>
    public int? TimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum output length in characters.
    /// </summary>
    public int? MaxOutputLength { get; set; }

    /// <summary>
    /// Gets or sets the status of the shell call.
    /// </summary>
    public string? Status { get; set; }
}
