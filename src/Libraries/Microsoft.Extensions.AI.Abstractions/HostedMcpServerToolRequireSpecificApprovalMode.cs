// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
        HashCode.Combine(GetListHashCode(AlwaysRequireApprovalToolNames), GetListHashCode(NeverRequireApprovalToolNames));

    private static bool ListEquals(IList<string>? list1, IList<string>? list2) =>
        ReferenceEquals(list1, list2) ||
        (list1 is not null && list2 is not null && list1.SequenceEqual(list2));

    private static int GetListHashCode(IList<string>? list)
    {
        if (list is null)
        {
            return 0;
        }

        HashCode hc = default;
        foreach (string item in list)
        {
            hc.Add(item);
        }

        return hc.ToHashCode();
    }
}
