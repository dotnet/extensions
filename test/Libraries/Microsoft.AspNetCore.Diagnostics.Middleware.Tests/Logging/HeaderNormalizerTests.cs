// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

public class HeaderNormalizerTests
{
    [Fact]
    public void NormalizeHeaderNameTest()
    {
        Assert.Equal("accept_charset", HeaderNormalizer.Normalize("Accept-Charset"));
    }
}
