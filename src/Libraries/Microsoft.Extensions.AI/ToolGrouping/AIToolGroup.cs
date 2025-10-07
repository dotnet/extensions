// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a logical grouping of tools that can be dynamically expanded.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="AIToolGroup"/> is an <see cref="AITool"/> that supplies an ordered list of <see cref="AITool"/> instances
/// via the <see cref="GetToolsAsync"/> method. This enables grouping tools together for organizational purposes
/// and allows for dynamic tool selection based on context.
/// </para>
/// <para>
/// Tool groups can be used independently or in conjunction with <see cref="ToolGroupingChatClient"/> to implement
/// hierarchical tool selection, where groups are initially collapsed and can be expanded on demand.
/// </para>
/// </remarks>
[Experimental("MEAI001")]
public abstract class AIToolGroup : AITool
{
    private readonly string _name;
    private readonly string _description;

    /// <summary>Initializes a new instance of the <see cref="AIToolGroup"/> class.</summary>
    /// <param name="name">Group name (identifier used by the expansion function).</param>
    /// <param name="description">Human readable description of the group.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    protected AIToolGroup(string name, string description)
    {
        _name = Throw.IfNull(name);
        _description = Throw.IfNull(description);
    }

    /// <summary>Gets the group name.</summary>
    public override string Name => _name;

    /// <summary>Gets the group description.</summary>
    public override string Description => _description;

    /// <summary>Creates a tool group with a static list of tools.</summary>
    /// <param name="name">Group name (identifier used by the expansion function).</param>
    /// <param name="description">Human readable description of the group.</param>
    /// <param name="tools">Ordered tools contained in the group.</param>
    /// <returns>An <see cref="AIToolGroup"/> instance containing the specified tools.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="tools"/> is <see langword="null"/>.</exception>
    public static AIToolGroup Create(string name, string description, IReadOnlyList<AITool> tools)
    {
        _ = Throw.IfNull(name);
        _ = Throw.IfNull(tools);
        return new StaticAIToolGroup(name, description, tools);
    }

    /// <summary>
    /// Asynchronously retrieves the ordered list of tools belonging to this group.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, containing the ordered list of tools in the group.</returns>
    /// <remarks>
    /// The returned list may contain other <see cref="AIToolGroup"/> instances, enabling hierarchical tool organization.
    /// Implementations should ensure the returned list is stable and deterministic for a given group instance.
    /// </remarks>
    public abstract ValueTask<IReadOnlyList<AITool>> GetToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>A tool group implementation that returns a static list of tools.</summary>
    private sealed class StaticAIToolGroup : AIToolGroup
    {
        private readonly IReadOnlyList<AITool> _tools;

        public StaticAIToolGroup(string name, string description, IReadOnlyList<AITool> tools)
            : base(name, description)
        {
            _tools = tools;
        }

        public override ValueTask<IReadOnlyList<AITool>> GetToolsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ValueTask<IReadOnlyList<AITool>>(_tools);
        }
    }
}
