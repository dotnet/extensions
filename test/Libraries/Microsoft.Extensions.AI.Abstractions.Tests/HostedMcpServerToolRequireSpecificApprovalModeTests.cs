// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedMcpServerToolRequireSpecificApprovalModeTests
{
    public static IEnumerable<object[]> RequireSpecific_Equality_MemberData()
    {
        // Both null sets
        yield return new object[]
        {
            new HostedMcpServerToolRequireSpecificApprovalMode(null, null),
            new HostedMcpServerToolRequireSpecificApprovalMode(null, null),
            true
        };

        // Empty sets vs null sets
        yield return new object[]
        {
            new HostedMcpServerToolRequireSpecificApprovalMode(new HashSet<string>(), new HashSet<string>()),
            new HostedMcpServerToolRequireSpecificApprovalMode(null, null),
            false
        };

        // Different null sets
        yield return new object[]
        {
            new HostedMcpServerToolRequireSpecificApprovalMode(
                new HashSet<string> { "tool1" },
                null),
            new HostedMcpServerToolRequireSpecificApprovalMode(
                null,
                new HashSet<string> { "tool1" }),
            false
        };

        // Different comparers (case-sensitive vs case-insensitive)
        yield return new object[]
        {
            new HostedMcpServerToolRequireSpecificApprovalMode(
                new HashSet<string>(StringComparer.Ordinal) { "Tool1" },
                null),
            new HostedMcpServerToolRequireSpecificApprovalMode(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Tool1" },
                null),
            false
        };

        // Same content, different order
        yield return new object[]
        {
            new HostedMcpServerToolRequireSpecificApprovalMode(
                new HashSet<string> { "tool1", "tool2" },
                new HashSet<string> { "tool3", "tool4" }),
            new HostedMcpServerToolRequireSpecificApprovalMode(
                new HashSet<string> { "tool2", "tool1" },
                new HashSet<string> { "tool4", "tool3" }),
            true
        };

        // Different content
        yield return new object[]
        {
            new HostedMcpServerToolRequireSpecificApprovalMode(
                new HashSet<string> { "tool1" },
                new HashSet<string> { "tool2" }),
            new HostedMcpServerToolRequireSpecificApprovalMode(
                new HashSet<string> { "tool1" },
                new HashSet<string> { "tool3" }),
            false
        };

        // Reference equality (same instance)
        var instance = new HostedMcpServerToolRequireSpecificApprovalMode(
            new HashSet<string> { "tool1" },
            new HashSet<string> { "tool2" });
        yield return new object[]
        {
            instance,
            instance,
            true
        };
    }

    [Theory]
    [MemberData(nameof(RequireSpecific_Equality_MemberData))]
    public void RequireSpecific_Equality_TestCases(
        HostedMcpServerToolRequireSpecificApprovalMode left,
        HostedMcpServerToolRequireSpecificApprovalMode right,
        bool expected)
    {
        Assert.Equal(expected, left.Equals(right));
        Assert.Equal(expected, right.Equals(left));
        Assert.Equal(expected, left.GetHashCode() == right.GetHashCode());
    }
}
