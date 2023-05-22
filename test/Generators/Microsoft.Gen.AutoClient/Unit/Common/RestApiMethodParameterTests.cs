// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Gen.AutoClient.Model;
using Xunit;

namespace Microsoft.Gen.AutoClient.Test;

public class RestApiMethodParameterTests
{
    [Fact]
    public void Fields_Should_BeInitialized()
    {
        var instance = new RestApiMethodParameter();
        Assert.Empty(instance.Name);
        Assert.Empty(instance.Type);
        Assert.Null(instance.HeaderName);
        Assert.Null(instance.QueryKey);
        Assert.Null(instance.BodyType);
        Assert.False(instance.IsHeader);
        Assert.False(instance.IsQuery);
        Assert.False(instance.IsBody);
    }
}
