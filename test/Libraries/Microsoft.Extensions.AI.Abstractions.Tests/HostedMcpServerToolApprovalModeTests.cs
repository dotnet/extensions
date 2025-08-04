// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedMcpServerToolApprovalModeTests
{
    [Fact]
    public void Singletons_Idempotent()
    {
        Assert.Same(HostedMcpServerToolApprovalMode.AlwaysRequire, HostedMcpServerToolApprovalMode.AlwaysRequire);
        Assert.Same(HostedMcpServerToolApprovalMode.NeverRequire, HostedMcpServerToolApprovalMode.NeverRequire);
    }
}
