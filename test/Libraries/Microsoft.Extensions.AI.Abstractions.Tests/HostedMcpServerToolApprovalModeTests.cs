// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedMcpServerToolApprovalModeTests
{
    [Fact]
    public void Constructor_Roundtrips()
    {
        IList<string> require = [];
        IList<string> notRequire = [];

        var approvalMode = new HostedMcpServerToolApprovalMode(require, notRequire);

        Assert.NotNull(approvalMode);
        Assert.Same(require, approvalMode.Require);
        Assert.Same(notRequire, approvalMode.NotRequire);
    }

    [Fact]
    public void Constructor_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new HostedMcpServerToolApprovalMode(null!, []));
        Assert.Throws<ArgumentNullException>(() => new HostedMcpServerToolApprovalMode([], null!));
    }
}
