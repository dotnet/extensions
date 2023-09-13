// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Compliance.Testing.Tests;

public static class InstancesTest
{
    [Fact]
    public static void Basic()
    {
        Assert.Equal(FakeTaxonomy.PrivateData, (FakeTaxonomy)FakeClassifications.PrivateData.Value);
        Assert.Equal(FakeTaxonomy.PublicData, (FakeTaxonomy)FakeClassifications.PublicData.Value);
    }
}
