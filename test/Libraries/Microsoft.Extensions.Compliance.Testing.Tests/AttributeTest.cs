// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Classification;
using Xunit;

namespace Microsoft.Extensions.Compliance.Testing.Tests;

public static class AttributeTest
{
    [Fact]
    public static void Basic()
    {
        DataClassificationAttribute a;

        a = new PrivateDataAttribute();
        Assert.Equal(FakeClassifications.PrivateData, a.Classification);

        a = new PublicDataAttribute();
        Assert.Equal(FakeClassifications.PublicData, a.Classification);
    }
}
