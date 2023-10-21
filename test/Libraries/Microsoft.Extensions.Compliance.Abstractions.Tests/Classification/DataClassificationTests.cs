// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Compliance.Classification.Tests;

public static class DataClassificationTests
{
    [Fact]
    public static void Basic()
    {
        const string TaxonomyName = "MyTaxonomy";
        const string Value = "123";

        var dc = new DataClassification(TaxonomyName, Value);
        Assert.Equal(TaxonomyName, dc.TaxonomyName);
        Assert.Equal(Value, dc.Value);

        Assert.True(dc == new DataClassification(TaxonomyName, Value));
        Assert.False(dc != new DataClassification(TaxonomyName, Value));

        Assert.True(dc != new DataClassification(TaxonomyName + "x", Value));
        Assert.False(dc == new DataClassification(TaxonomyName + "x", Value));

        Assert.True(dc != new DataClassification(TaxonomyName, Value + 1));
        Assert.False(dc == new DataClassification(TaxonomyName, Value + 1));

        Assert.True(dc.Equals((object)dc));
        Assert.False(dc.Equals(new object()));

        Assert.Equal(dc.GetHashCode(), dc.GetHashCode());
        Assert.NotEqual(dc.GetHashCode(), new DataClassification(TaxonomyName + "X", Value).GetHashCode());
        Assert.NotEqual(dc.GetHashCode(), new DataClassification(TaxonomyName, Value + 1).GetHashCode());

        Assert.False(dc.Equals(null));
    }

    [Fact]
    public static void ToStringOutput()
    {
        const string TaxonomyName = "MyTaxonomy";
        const string Value = "0x0123";

        var dc = new DataClassification(TaxonomyName, Value);

        Assert.Equal($"{TaxonomyName}:{Value}", dc.ToString());
    }
}
