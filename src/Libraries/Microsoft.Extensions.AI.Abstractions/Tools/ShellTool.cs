// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a shell tool that can execute commands and be described to an AI service.</summary>
/// <remarks>
/// <para>
/// This is an abstract base class for shell tools that execute commands locally.
/// Subclasses must implement <see cref="AIFunction.InvokeCoreAsync"/> to provide the actual command execution logic.
/// </para>
/// <para>
/// <see cref="IChatClient"/> implementations backed by a service that has its own notion of a shell tool
/// can special-case this type, translating it into usage of the service's native shell tool.
/// For <see cref="IChatClient"/> implementations without such special-casing, the tool functions as
/// a standard <see cref="AIFunction"/> that can be invoked via <see cref="AIFunction.InvokeAsync"/>.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIShell, UrlFormat = DiagnosticIds.UrlFormat)]
public abstract class ShellTool : AIFunction
{
    /// <summary>Any additional properties associated with the tool.</summary>
    private IReadOnlyDictionary<string, object?>? _additionalProperties;

    /// <summary>Initializes a new instance of the <see cref="ShellTool"/> class.</summary>
    protected ShellTool()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ShellTool"/> class.</summary>
    /// <param name="additionalProperties">Any additional properties associated with the tool.</param>
    protected ShellTool(IReadOnlyDictionary<string, object?>? additionalProperties)
    {
        _additionalProperties = additionalProperties;
    }

    /// <inheritdoc />
    public override string Name => "local_shell";

    /// <inheritdoc />
    public override string Description => "Executes a shell command and returns stdout, stderr, and exit code.";

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> AdditionalProperties => _additionalProperties ?? base.AdditionalProperties;
}
