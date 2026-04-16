// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a hosted tool that can be specified to an AI service to enable it to search for and selectively load tool definitions on demand.</summary>
/// <remarks>
/// <para>
/// This tool does not itself implement tool search. It is a marker that can be used to inform a service
/// that tool search should be enabled, reducing token usage by deferring full tool schema loading until the model requests it.
/// </para>
/// <para>
/// By default, when a <see cref="HostedToolSearchTool"/> is present in the tools list, all other tools are treated
/// as having deferred loading enabled. Use <see cref="DeferredTools"/> to control which tools have deferred loading
/// on a per-tool basis.
/// </para>
/// </remarks>
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
    /// When non-null, only tools whose names appear in this list will have deferred loading enabled.
    /// </para>
    /// </remarks>
    public IList<string>? DeferredTools { get; set; }

    /// <summary>
    /// Gets or sets the namespace name under which deferred tools should be grouped.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When non-null, all deferred function tools are wrapped inside a <c>{"type":"namespace","name":"..."}</c>
    /// container. Non-deferred tools remain as top-level tools.
    /// </para>
    /// <para>
    /// When <see langword="null"/> (the default), deferred tools are sent as top-level function tools
    /// with <c>defer_loading</c> set individually.
    /// </para>
    /// </remarks>
    public string? Namespace { get; set; }
}
