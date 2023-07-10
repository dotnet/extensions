// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Compliance.Classification.Tests;

public static class DataClassificationTest
{
    [Fact]
    public static void Basic()
    {
        const string TaxonomyName = "MyTaxonomy";
        const ulong Mask = 123;

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
    public static void Combine()
    {
        const string TaxonomyName = "MyTaxonomy";
        const ulong Mask1 = 0x0123;
        const ulong Mask2 = 0x8000;

        var dc1 = new DataClassification(TaxonomyName, Mask1);
        var dc2 = new DataClassification(TaxonomyName, Mask2);

        Assert.Equal(Mask1 | Mask2, (dc1 | dc2).Value);
        Assert.Equal(Mask1 | Mask2, (dc2 | dc1).Value);
        Assert.Throws<ArgumentException>(() => dc1 | new DataClassification(TaxonomyName + "X", Mask2));

        Assert.Equal(DataClassification.UnknownTaxonomyValue, (dc1 | DataClassification.Unknown).Value);
        Assert.Equal(string.Empty, (dc1 | DataClassification.Unknown).TaxonomyName);

        Assert.Equal(dc1.Value, (dc1 | DataClassification.None).Value);
        Assert.Equal(dc1.TaxonomyName, (dc1 | DataClassification.None).TaxonomyName);

        Assert.Equal(DataClassification.UnknownTaxonomyValue, (DataClassification.Unknown | dc1).Value);
        Assert.Equal(string.Empty, (dc1 | DataClassification.Unknown).TaxonomyName);

        Assert.Equal(dc1.Value, (DataClassification.None | dc1).Value);
        Assert.Equal(dc1.TaxonomyName, (DataClassification.None | dc1).TaxonomyName);
    }

    [Fact]
    public static void ToStringOutput()
    {
        const string TaxonomyName = "MyTaxonomy";
        const ulong Mask = 0x0123;

        var dc = new DataClassification(TaxonomyName, Mask);

        Assert.Equal($"{TaxonomyName}:{Mask:x}", dc.ToString());
    }
}
