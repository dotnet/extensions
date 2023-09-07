// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Gen.Metrics.Model;
using Xunit;

namespace Microsoft.Gen.Metrics.Test;

public class StrongTypeConfigTests
{
    [Fact]
    public void Fields_Should_BeInitialized()
    {
        var instance = new StrongTypeConfig();
        Assert.Empty(instance.Name);
        Assert.Empty(instance.Path);
        Assert.Empty(instance.TagName);
    }
}
