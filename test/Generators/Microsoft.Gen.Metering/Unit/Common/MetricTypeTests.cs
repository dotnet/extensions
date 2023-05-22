// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Gen.Metering.Model;
using Xunit;

namespace Microsoft.Gen.Metering.Test;

public class MetricTypeTests
{
    [Fact]
    public void Fields_Should_BeInitialized()
    {
        var instance = new MetricType();
        Assert.Empty(instance.Name);
        Assert.Empty(instance.Namespace);
        Assert.Empty(instance.Constraints);
        Assert.Empty(instance.Modifiers);
        Assert.Empty(instance.Keyword);
    }
}
