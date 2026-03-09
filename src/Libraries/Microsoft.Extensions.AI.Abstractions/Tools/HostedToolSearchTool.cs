// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a hosted tool that can be specified to an AI service to enable it to search for and selectively load tool definitions on demand.</summary>
/// <remarks>
/// <para>
/// This tool does not itself implement tool search. It is a marker that can be used to inform a service
/// that tool search should be enabled, reducing token usage by deferring full tool schema loading until the model requests it.
/// </para>
/// <para>
/// By default, when a <see cref="HostedToolSearchTool"/> is present in the tools list, all other tools are treated
/// as having deferred loading enabled. Use <see cref="DeferredTools"/> and <see cref="NonDeferredTools"/> to control
/// which tools have deferred loading on a per-tool basis.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIToolSearch, UrlFormat = DiagnosticIds.UrlFormat)]
public class HostedToolSearchTool : AITool
{
    /// <summary>Any additional properties associated with the tool.</summary>
    private IReadOnlyDictionary<string, object?>? _additionalProperties;

    /// <summary>Initializes a new instance of the <see cref="HostedToolSearchTool"/> class.</summary>
    public HostedToolSearchTool()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="HostedToolSearchTool"/> class.</summary>
    /// <param name="additionalProperties">Any additional properties associated with the tool.</param>
    public HostedToolSearchTool(IReadOnlyDictionary<string, object?>? additionalProperties)
    {
        _additionalProperties = additionalProperties;
    }

    /// <inheritdoc />
    public override string Name => "tool_search";

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> AdditionalProperties => _additionalProperties ?? base.AdditionalProperties;

    /// <summary>
    /// Gets or sets the list of tool names for which deferred loading should be enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default value is <see langword="null"/>, which enables deferred loading for all tools in the tools list.
    /// </para>
    /// <para>
    /// When non-null, only tools whose names appear in this list will have deferred loading enabled,
    /// unless they also appear in <see cref="NonDeferredTools"/>.
    /// </para>
    /// </remarks>
    public IList<string>? DeferredTools { get; set; }

    /// <summary>
    /// Gets or sets the list of tool names for which deferred loading should be disabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default value is <see langword="null"/>, which means no tools are excluded from deferred loading.
    /// </para>
    /// <para>
    /// When non-null, tools whose names appear in this list will not have deferred loading enabled,
    /// even if they also appear in <see cref="DeferredTools"/>.
    /// </para>
    /// </remarks>
    public IList<string>? NonDeferredTools { get; set; }
}
