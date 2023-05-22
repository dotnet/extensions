// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Classification;
using Xunit;

namespace Microsoft.Extensions.Compliance.Testing.Tests;

public static class TaxonomyExtensionsTest
{
    [Fact]
    public static void AsSimpleTaxonomy()
    {
        var dc = new DataClassification("Foo", 123);
        Assert.Throws<ArgumentException>(() => dc.AsSimpleTaxonomy());

        Assert.Equal(SimpleTaxonomy.None, DataClassification.None.AsSimpleTaxonomy());
        Assert.Equal(SimpleTaxonomy.Unknown, DataClassification.Unknown.AsSimpleTaxonomy());
    }
}
