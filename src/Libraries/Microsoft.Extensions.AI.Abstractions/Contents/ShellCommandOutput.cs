// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the output of a single shell command execution.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIShell, UrlFormat = DiagnosticIds.UrlFormat)]
public class ShellCommandOutput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShellCommandOutput"/> class.
    /// </summary>
    public ShellCommandOutput()
    {
    }

    /// <summary>
    /// Gets or sets the standard output of the command.
    /// </summary>
    public string? Stdout { get; set; }

    /// <summary>
    /// Gets or sets the standard error output of the command.
    /// </summary>
    public string? Stderr { get; set; }

    /// <summary>
    /// Gets or sets the exit code of the command, or <see langword="null"/> if the command timed out.
    /// </summary>
    public int? ExitCode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the command execution timed out.
    /// </summary>
    public bool TimedOut { get; set; }
}
