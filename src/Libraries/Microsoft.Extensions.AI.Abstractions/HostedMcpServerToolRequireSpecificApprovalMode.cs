// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a mode where approval behavior is specified for individual tool names.
/// </summary>
public sealed class HostedMcpServerToolRequireSpecificApprovalMode : HostedMcpServerToolApprovalMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostedMcpServerToolRequireSpecificApprovalMode"/> class that specifies approval behavior for individual tool names.
    /// </summary>
    /// <param name="alwaysRequireApprovalToolNames">The list of tools names that always require approval.</param>
    /// <param name="neverRequireApprovalToolNames">The list of tools names that never require approval.</param>
    public HostedMcpServerToolRequireSpecificApprovalMode(HashSet<string>? alwaysRequireApprovalToolNames, HashSet<string>? neverRequireApprovalToolNames)
    {
        AlwaysRequireApprovalToolNames = alwaysRequireApprovalToolNames;
        NeverRequireApprovalToolNames = neverRequireApprovalToolNames;
    }

    /// <summary>
    /// Gets or sets the list of tool names that always require approval.
    /// </summary>
    public HashSet<string>? AlwaysRequireApprovalToolNames { get; set; }

    /// <summary>
    /// Gets or sets the list of tool names that never require approval.
    /// </summary>
    public HashSet<string>? NeverRequireApprovalToolNames { get; set; }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is HostedMcpServerToolRequireSpecificApprovalMode other &&
        SetEquals(AlwaysRequireApprovalToolNames, other.AlwaysRequireApprovalToolNames) &&
        SetEquals(NeverRequireApprovalToolNames, other.NeverRequireApprovalToolNames);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(GetSetHashCode(AlwaysRequireApprovalToolNames), GetSetHashCode(NeverRequireApprovalToolNames));

    private static bool SetEquals(HashSet<string>? set1, HashSet<string>? set2)
    {
        if (ReferenceEquals(set1, set2))
        {
            return true;
        }

        if (set1 is null || set2 is null ||
            !ReferenceEquals(set1.Comparer, set2.Comparer))
        {
            return false;
        }

        return set1.SetEquals(set2);
    }

    private static int GetSetHashCode(HashSet<string>? set)
    {
        if (set is null)
        {
            return 0;
        }

        int xor = 0;
        int sum = 0;
        foreach (string item in set)
        {
            int hash = set.Comparer.GetHashCode(item);
            xor ^= hash;
            sum += hash;
        }

        return HashCode.Combine(set.Comparer, xor, sum);
    }
}
