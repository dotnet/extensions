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
        var dc1 = new DataClassificationSet(FakeClassifications.PublicData);
        var dc2 = new DataClassificationSet(new[] { FakeClassifications.PublicData });
        var dc3 = new DataClassificationSet(new List<DataClassification> { FakeClassifications.PublicData });
        var dc4 = (DataClassificationSet)FakeClassifications.PublicData;
        var dc5 = DataClassificationSet.FromDataClassification(FakeClassifications.PublicData);

        Assert.Equal(dc1, dc2);
        Assert.Equal(dc1, dc3);
        Assert.Equal(dc1, dc4);
        Assert.Equal(dc1, dc5);

        var dc6 = dc1.Union(FakeClassifications.PrivateData);
        Assert.NotEqual(dc1, dc6);
    }
}
