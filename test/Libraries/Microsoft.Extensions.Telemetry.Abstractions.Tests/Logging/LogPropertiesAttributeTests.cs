// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Logging.Test;

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
    public void OmiReferenceName()
    {
        var lpa = new LogPropertiesAttribute();
        Assert.False(lpa.OmitReferenceName);

        lpa.OmitReferenceName = true;
        Assert.True(lpa.OmitReferenceName);
    }

    [Fact]
    public void Transitive()
    {
        var lpa = new LogPropertiesAttribute();
        Assert.False(lpa.Transitive);

        lpa.Transitive = true;
        Assert.True(lpa.Transitive);
    }
}
