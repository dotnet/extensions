// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.AI;

public class DecisionClientExceptionTests
{
    [Fact]
    public void ClientException_ReportsTransience()
    {
        Assert.False(new DecisionClientException("failure").IsTransient);
        Assert.True(new DecisionClientException("throttled", isTransient: true).IsTransient);
    }
}