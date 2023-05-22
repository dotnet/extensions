// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Logging.Test;

public class LogPropertiesAttributeTests
{
    [Fact]
    public void SkipNullProps()
    {
        var lpa = new LogPropertiesAttribute();
        Assert.False(lpa.SkipNullProperties);

        lpa.SkipNullProperties = true;
        Assert.True(lpa.SkipNullProperties);
    }

    [Fact]
    public void OmitParameterName()
    {
        var lpa = new LogPropertiesAttribute();
        Assert.False(lpa.OmitParameterName);

        lpa.OmitParameterName = true;
        Assert.True(lpa.OmitParameterName);
    }

    [Fact]
    public void ShouldThrow_WhenCtorArgument_IsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new LogPropertiesAttribute(null!, "test"));
        Assert.Throws<ArgumentNullException>(() => new LogPropertiesAttribute(typeof(object), null!));
    }

    [Fact]
    public void ShouldThrow_WhenMethodIsEmptyOrWhitespace()
    {
        Assert.Throws<ArgumentException>(() => new LogPropertiesAttribute(typeof(object), string.Empty));
        Assert.Throws<ArgumentException>(() => new LogPropertiesAttribute(typeof(object), new string(' ', 3)));
    }

    [Fact]
    public void ShouldSet_Properties_WhenCustomProvider()
    {
        const string ProviderMethod = "test_method";

        var attr = new LogPropertiesAttribute(typeof(DateTime), ProviderMethod);
        Assert.Equal(typeof(DateTime), attr.ProviderType);
        Assert.Equal(ProviderMethod, attr.ProviderMethod);
    }

    [Fact]
    public void ShouldNotSet_Properties_WhenDefaultProvider()
    {
        var attr = new LogPropertiesAttribute();
        Assert.Null(attr.ProviderType);
        Assert.Null(attr.ProviderMethod);
    }
}
