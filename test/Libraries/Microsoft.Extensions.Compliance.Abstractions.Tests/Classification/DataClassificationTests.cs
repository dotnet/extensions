// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Compliance.Classification.Tests;

public static class DataClassificationTests
{
    [Fact]
    public static void Basic()
    {
        const string TaxonomyName = "MyTaxonomy";
        const long Mask = 123;

        var dc = new DataClassification(TaxonomyName, Mask);
        Assert.Equal(TaxonomyName, dc.TaxonomyName);
        Assert.Equal(Mask, dc.Value);

        Assert.True(dc == new DataClassification(TaxonomyName, Mask));
        Assert.False(dc != new DataClassification(TaxonomyName, Mask));

        Assert.True(dc != new DataClassification(TaxonomyName + "x", Mask));
        Assert.False(dc == new DataClassification(TaxonomyName + "x", Mask));

        Assert.True(dc != new DataClassification(TaxonomyName, Mask + 1));
        Assert.False(dc == new DataClassification(TaxonomyName, Mask + 1));

        Assert.True(dc.Equals((object)dc));
        Assert.False(dc.Equals(new object()));

        Assert.Equal(dc.GetHashCode(), dc.GetHashCode());
        Assert.NotEqual(dc.GetHashCode(), new DataClassification(TaxonomyName + "X", Mask).GetHashCode());
        Assert.NotEqual(dc.GetHashCode(), new DataClassification(TaxonomyName, Mask + 1).GetHashCode());
    }

    [Fact]
    public static void CantCreateUnknownClassifications()
    {
        Assert.Throws<ArgumentException>(() => new DataClassification("Foo", DataClassification.None.Value));
        Assert.Throws<ArgumentException>(() => new DataClassification("Foo", DataClassification.Unknown.Value));
    }

    [Fact]
    public static void ToStringOutput()
    {
        const string TaxonomyName = "MyTaxonomy";
        const long Mask = 0x0123;

        var dc = new DataClassification(TaxonomyName, Mask);

        Assert.Equal($"{TaxonomyName}:{Mask:x}", dc.ToString());
    }
}
