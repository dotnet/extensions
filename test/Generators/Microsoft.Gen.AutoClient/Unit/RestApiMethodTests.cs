// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Gen.AutoClient.Model;
using Xunit;

namespace Microsoft.Gen.AutoClient.Test;

public class RestApiMethodTests
{
    [Fact]
    public void Fields_Should_BeInitialized()
    {
        var instance = new RestApiMethod();
        Assert.Empty(instance.AllParameters);
        Assert.Empty(instance.FormatParameters);
        Assert.Empty(instance.MethodName);
        Assert.Empty(instance.HttpMethod!);
        Assert.Empty(instance.Path!);
        Assert.Empty(instance.ReturnType!);
        Assert.Empty(instance.RequestName);
    }
}
