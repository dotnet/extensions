// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DecisionUnansweredReasonTests
{
    [Fact]
    public void UnansweredReason_UsesCaseInsensitiveEquality()
    {
        DecisionUnansweredReason custom = new("Provider_Filter");
        Assert.Equal(new DecisionUnansweredReason("provider_filter"), custom);
        Assert.True(custom != DecisionUnansweredReason.Refusal);
        Assert.Equal("refusal", DecisionUnansweredReason.Refusal.ToString());
        Assert.Equal("refusal", default(DecisionUnansweredReason).Value);
        Assert.ThrowsAny<ArgumentException>(() => new DecisionUnansweredReason(" "));
    }
}