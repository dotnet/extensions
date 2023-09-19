// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Gen.Metrics.Model;
using Xunit;

namespace Microsoft.Gen.Metrics.Test;

public class MetricMethodTests
{
    [Fact]
    public void Fields_Should_BeInitialized()
    {
        var instance = new MetricMethod();
        Assert.Empty(instance.Modifiers);
        Assert.Empty(instance.MetricTypeModifiers);
        Assert.Empty(instance.MetricTypeName);
        Assert.Empty(instance.GenericType);
    }
}
