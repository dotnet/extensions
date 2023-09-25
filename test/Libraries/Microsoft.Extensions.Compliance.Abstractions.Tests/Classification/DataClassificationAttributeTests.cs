// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Compliance.Classification.Tests;

public static class DataClassificationAttributeTests
{
    private const string TaxonomyName = "Tax";
    private const ulong Mask = 123;

    private sealed class TestAttribute : DataClassificationAttribute
    {
        public TestAttribute()
            : base(new DataClassification(TaxonomyName, Mask))
        {
        }
    }

    [Fact]
    public static void Basic()
    {
        var attribute = new TestAttribute();
        Assert.Equal(0, attribute.Notes.Length);
        Assert.True(attribute.Classification == new DataClassification(TaxonomyName, Mask));

        attribute.Notes = "Hello";
        Assert.Equal("Hello", attribute.Notes);
    }
}
