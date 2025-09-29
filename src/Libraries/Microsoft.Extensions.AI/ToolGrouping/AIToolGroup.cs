// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a logical grouping of tools that can be dynamically expanded.
/// </summary>
/// <remarks>
/// A <see cref="AIToolGroup"/> supplies a stable ordered list of <see cref="AITool"/> instances.
/// Group membership is determined by reference equality of the tool instances.
/// </remarks>
[Experimental("MEAI001")]
public sealed class AIToolGroup
{
    /// <summary>Initializes a new instance of the <see cref="AIToolGroup"/> class.</summary>
    /// <param name="name">Group name (identifier used by the expansion function).</param>
    /// <param name="description">Human readable description of the group.</param>
    /// <param name="tools">Ordered tools contained in the group.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public AIToolGroup(string name, string description, IReadOnlyList<AITool> tools)
    {
        Name = Throw.IfNull(name);
        Description = description ?? string.Empty;
        Tools = Throw.IfNull(tools);
    }

    /// <summary>Gets the group name.</summary>
    public string Name { get; }

    /// <summary>Gets the group description.</summary>
    public string Description { get; }

    /// <summary>Gets the ordered tools belonging to the group.</summary>
    public IReadOnlyList<AITool> Tools { get; }
}
