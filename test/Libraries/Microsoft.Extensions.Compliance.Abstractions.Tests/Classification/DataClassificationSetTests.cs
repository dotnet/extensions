// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Testing;
using Xunit;

namespace Microsoft.Extensions.Compliance.Classification.Tests;

public static class DataClassificationSetTests
{
    [Fact]
    public static void Basic()
    {
        var dc1 = new DataClassificationSet(FakeTaxonomy.PublicData);
        var dc2 = new DataClassificationSet(new[] { FakeTaxonomy.PublicData });
        var dc3 = new DataClassificationSet(new List<DataClassification> { FakeTaxonomy.PublicData });
        var dc4 = (DataClassificationSet)FakeTaxonomy.PublicData;
        var dc5 = DataClassificationSet.FromDataClassification(FakeTaxonomy.PublicData);

        Assert.Equal(dc1, dc2);
        Assert.Equal(dc1, dc3);
        Assert.Equal(dc1, dc4);
        Assert.Equal(dc1, dc5);

        var dc6 = dc1.Union(FakeTaxonomy.PrivateData);
        Assert.NotEqual(dc1, dc6);

#pragma warning disable CA1508 // Avoid dead conditional code
        Assert.False(dc1.Equals(null));
#pragma warning restore CA1508 // Avoid dead conditional code
    }

    [Fact]
    public static void TestHashCodes()
    {
        var dc1 = new DataClassificationSet(FakeTaxonomy.PublicData);
        var dc2 = new DataClassificationSet(new[] { FakeTaxonomy.PublicData });
        var dc3 = new DataClassificationSet(new List<DataClassification> { FakeTaxonomy.PublicData });
        var dc4 = (DataClassificationSet)FakeTaxonomy.PublicData;
        var dc5 = DataClassificationSet.FromDataClassification(FakeTaxonomy.PublicData);

        Assert.Equal(dc1.GetHashCode(), dc2.GetHashCode());
        Assert.Equal(dc1.GetHashCode(), dc3.GetHashCode());
        Assert.Equal(dc1.GetHashCode(), dc4.GetHashCode());
        Assert.Equal(dc1.GetHashCode(), dc5.GetHashCode());

        var dc6 = dc1.Union(FakeTaxonomy.PrivateData);
        Assert.NotEqual(dc1, dc6);
    }
}
