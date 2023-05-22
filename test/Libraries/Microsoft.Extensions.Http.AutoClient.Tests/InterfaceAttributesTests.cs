// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Http.AutoClient.Test;

public class InterfaceAttributesTests
{
    [Fact]
    public void StaticHeaderAttributeNameAndValue()
    {
        var a = new StaticHeaderAttribute("HeaderName", "value");
        Assert.Equal("HeaderName", a.Header);
        Assert.Equal("value", a.Value);
    }

    [Fact]
    public void RestApiAttributeClientNameAndDependencyName()
    {
        var a = new AutoClientAttribute("MyClient");
        Assert.Equal("MyClient", a.HttpClientName);

        a = new AutoClientAttribute("MyClient1", "MyDependency");
        Assert.Equal("MyClient1", a.HttpClientName);
        Assert.Equal("MyDependency", a.CustomDependencyName);
    }
}
