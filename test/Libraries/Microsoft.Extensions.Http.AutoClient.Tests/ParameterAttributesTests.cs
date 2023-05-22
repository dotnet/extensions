// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Http.AutoClient.Test;

public class ParameterAttributesTests
{
    [Fact]
    public void BodyAttributeContentType()
    {
        var a = new BodyAttribute();
        Assert.Equal(BodyContentType.ApplicationJson, a.ContentType);

        a = new BodyAttribute(BodyContentType.TextPlain);
        Assert.Equal(BodyContentType.TextPlain, a.ContentType);
    }

    [Fact]
    public void HeaderAttributeName()
    {
        var a = new HeaderAttribute("HeaderName");
        Assert.Equal("HeaderName", a.Header);
    }

    [Fact]
    public void QueryAttributeNameOrNull()
    {
        var a = new QueryAttribute();
        Assert.Null(a.Key);

        a = new QueryAttribute("QueryKey");
        Assert.Equal("QueryKey", a.Key);
    }

    [Fact]
    public void RequestNameAttributeValue()
    {
        var a = new RequestNameAttribute("RequestName");
        Assert.Equal("RequestName", a.Value);
    }
}
