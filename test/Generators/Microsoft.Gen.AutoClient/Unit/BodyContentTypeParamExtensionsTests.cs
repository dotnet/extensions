// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Gen.AutoClient.Model;
using Xunit;

namespace Microsoft.Gen.AutoClient.Test;

public class BodyContentTypeParamExtensionsTests
{
    [Fact]
    public void ConvertToString()
    {
        Assert.Equal("application/json", BodyContentTypeParamExtensions.ConvertToString(BodyContentTypeParam.ApplicationJson));
        Assert.Equal("text/plain", BodyContentTypeParamExtensions.ConvertToString(BodyContentTypeParam.TextPlain));
        Assert.Equal("", BodyContentTypeParamExtensions.ConvertToString((BodyContentTypeParam)999));
    }
}
