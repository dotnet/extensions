// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Logging.Test;

public class TagProviderAttributeTests
{
    [Fact]
    public void OmitReferenceName()
    {
        const string ProviderMethod = "test_method";

        var attr = new TagProviderAttribute(typeof(DateTime), ProviderMethod);
        Assert.False(attr.OmitReferenceName);

        attr.OmitReferenceName = true;
        Assert.True(attr.OmitReferenceName);
    }

    [Fact]
    public void ShouldThrow_WhenCtorArgument_IsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new TagProviderAttribute(null!, "test"));
        Assert.Throws<ArgumentNullException>(() => new TagProviderAttribute(typeof(object), null!));
    }

    [Fact]
    public void ShouldThrow_WhenMethodIsEmptyOrWhitespace()
    {
        Assert.Throws<ArgumentException>(() => new TagProviderAttribute(typeof(object), string.Empty));
        Assert.Throws<ArgumentException>(() => new TagProviderAttribute(typeof(object), new string(' ', 3)));
    }

    [Fact]
    public void ShouldSet_Properties_WhenCustomProvider()
    {
        const string ProviderMethod = "test_method";

        var attr = new TagProviderAttribute(typeof(DateTime), ProviderMethod);
        Assert.Equal(typeof(DateTime), attr.ProviderType);
        Assert.Equal(ProviderMethod, attr.ProviderMethod);
    }
}
