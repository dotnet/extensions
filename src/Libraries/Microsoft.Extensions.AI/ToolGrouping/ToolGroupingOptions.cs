// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

#pragma warning disable IDE0032 // Use auto property, suppressed until repo updates to C# 14

namespace Microsoft.Extensions.AI;

/// <summary>Options controlling tool grouping / expansion behavior.</summary>
[Experimental("MEAI001")]
public sealed class ToolGroupingOptions
{
    private string _expansionFunctionName = "__expand_tool_group";
    private string? _expansionFunctionDescription;
    private string _listGroupsFunctionName = "__list_tool_groups";
    private string? _listGroupsFunctionDescription;
    private int _maxExpansionsPerRequest = 3;

    /// <summary>Gets or sets the name of the synthetic expansion function tool.</summary>
    public string ExpansionFunctionName
    {
        get => _expansionFunctionName;
        set => _expansionFunctionName = Throw.IfNull(value);
    }

    /// <summary>Gets or sets the description of the synthetic expansion function tool.</summary>
    public string? ExpansionFunctionDescription
    {
        get => _expansionFunctionDescription;
        set => _expansionFunctionDescription = value;
    }

    /// <summary>Gets or sets the name of the synthetic list groups function tool.</summary>
    public string ListGroupsFunctionName
    {
        get => _listGroupsFunctionName;
        set => _listGroupsFunctionName = Throw.IfNull(value);
    }

    /// <summary>Gets or sets the description of the synthetic list groups function tool.</summary>
    public string? ListGroupsFunctionDescription
    {
        get => _listGroupsFunctionDescription;
        set => _listGroupsFunctionDescription = value;
    }

    /// <summary>Gets or sets the maximum number of expansions allowed within a single request.</summary>
    /// <remarks>Defaults to 1 to keep the prototype deterministic and avoid runaway loops.</remarks>
    public int MaxExpansionsPerRequest
    {
        get => _maxExpansionsPerRequest;
        set => _maxExpansionsPerRequest = Throw.IfLessThan(value, 1);
    }
}
