// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Http.Latency.Internal;
using Xunit;

namespace Microsoft.Extensions.Http.Latency.Test.Internal;

public class HttpCheckpointsTest
{
    [Fact]
    public void HttpCheckpoints_ContainsList()
    {
        Assert.NotNull(HttpCheckpoints.Checkpoints);
        Assert.True(HttpCheckpoints.Checkpoints.Length > 0);
    }
}
