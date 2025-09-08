// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#pragma warning disable S109 // Magic numbers should not be used
#pragma warning disable EA0011 // Consider removing unnecessary conditional access operator (?)

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a mode where approval behavior is specified for individual tool names.
/// </summary>
[Experimental("MEAI001")]
public sealed class HostedMcpServerToolRequireSpecificApprovalMode : HostedMcpServerToolApprovalMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostedMcpServerToolRequireSpecificApprovalMode"/> class that specifies approval behavior for individual tool names.
    /// </summary>
    /// <param name="alwaysRequireApprovalToolNames">The list of tools names that always require approval.</param>
    /// <param name="neverRequireApprovalToolNames">The list of tools names that never require approval.</param>
    public HostedMcpServerToolRequireSpecificApprovalMode(IList<string>? alwaysRequireApprovalToolNames, IList<string>? neverRequireApprovalToolNames)
    {
        AlwaysRequireApprovalToolNames = alwaysRequireApprovalToolNames;
        NeverRequireApprovalToolNames = neverRequireApprovalToolNames;
    }

    /// <summary>
    /// Gets or sets the list of tool names that always require approval.
    /// </summary>
    public IList<string>? AlwaysRequireApprovalToolNames { get; set; }

    /// <summary>
    /// Gets or sets the list of tool names that never require approval.
    /// </summary>
    public IList<string>? NeverRequireApprovalToolNames { get; set; }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is HostedMcpServerToolRequireSpecificApprovalMode other &&
        ListEquals(AlwaysRequireApprovalToolNames, other.AlwaysRequireApprovalToolNames) &&
        ListEquals(NeverRequireApprovalToolNames, other.NeverRequireApprovalToolNames);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        Combine(GetListHashCode(AlwaysRequireApprovalToolNames), GetListHashCode(NeverRequireApprovalToolNames));

    private static bool ListEquals(IList<string>? list1, IList<string>? list2) =>
        ReferenceEquals(list1, list2) ||
        (list1 is not null && list2 is not null && list1.SequenceEqual(list2));

    private static int GetListHashCode(IList<string>? list)
    {
        if (list is null)
        {
            return 0;
        }

#if NET
        HashCode hc = default;
        for (int i = 0; i < list.Count; i++)
        {
            hc.Add(list[i]);
        }

        return hc.ToHashCode();
#else
        int hash = 0;
        foreach (string item in list)
        {
            hash = Combine(hash, item?.GetHashCode() ?? 0);
        }

        return hash;
#endif
    }

    private static int Combine(int h1, int h2)
    {
#if NET
        return HashCode.Combine(h1, h2);
#else
        uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
        return ((int)rol5 + h1) ^ h2;
#endif
    }
}
