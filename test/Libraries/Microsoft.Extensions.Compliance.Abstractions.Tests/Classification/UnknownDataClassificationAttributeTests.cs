// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Compliance.Classification.Tests;

public static class UnknownDataClassificationAttributeTests
{
    [Fact]
    public static void Basic()
    {
        var attribute = new UnknownDataClassificationAttribute();
        Assert.Equal(DataClassification.Unknown, attribute.Classification);
    }
}
