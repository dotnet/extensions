// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.Resilience.FaultInjection.Test.Options;

public class FaultInjectionOptionsTest
{
    [Fact]
    public void CanConstruct()
    {
        var instance = new FaultInjectionOptions();
        Assert.NotNull(instance);
    }

    [Fact]
    public void CanGetAndSetChaosPolicyOptionsGroups()
    {
        var chaosPolicyOptionsGroups = new Dictionary<string, ChaosPolicyOptionsGroup>();
        var instance = new FaultInjectionOptions
        {
            ChaosPolicyOptionsGroups = chaosPolicyOptionsGroups
        };

        Assert.Equal(chaosPolicyOptionsGroups, instance.ChaosPolicyOptionsGroups);
    }
}
