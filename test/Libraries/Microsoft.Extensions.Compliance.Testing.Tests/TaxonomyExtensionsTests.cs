// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Classification;
using Xunit;

namespace Microsoft.Extensions.Compliance.Testing.Tests;

public static class TaxonomyExtensionsTests
{
    [Fact]
    public static void AsSimpleTaxonomy()
    {
        var dc = new DataClassification("Foo", 123);
        Assert.Throws<ArgumentException>(() => dc.AsFakeTaxonomy());

        Assert.Equal(FakeTaxonomy.None, DataClassification.None.AsFakeTaxonomy());
        Assert.Equal(FakeTaxonomy.Unknown, DataClassification.Unknown.AsFakeTaxonomy());
    }
}
