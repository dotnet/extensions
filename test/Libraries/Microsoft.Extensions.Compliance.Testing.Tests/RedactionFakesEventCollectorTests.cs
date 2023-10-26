// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Compliance.Testing.Test;

public class RedactionFakesEventCollectorTests
{
    [Fact]
    public void When_No_Records_Cannot_Obtain_Last_Events()
    {
        var c = new FakeRedactionCollector();

        Assert.Throws<InvalidOperationException>(() => c.LastRedactorRequested);
        Assert.Throws<InvalidOperationException>(() => c.LastRedactedData);
    }

    [Fact]
    public void RedactionFakesEventCollector_Cannot_Be_Retrieved_From_DI_When_Not_Registered()
    {
        using var sp = new ServiceCollection().BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => sp.GetFakeRedactionCollector());
    }

    [Fact]
    public void DataRedacted_Collected_By_Collector_Support_Value_Semantic_Comparisons()
    {
        var first = new RedactedData(string.Empty, string.Empty, 0);
        var second = new RedactedData(string.Empty, string.Empty, 0);
        var third = new RedactedData("d", string.Empty, 0);
        var fourth = new RedactedData(string.Empty, string.Empty, 1);
        var fifth = new RedactedData(string.Empty, "d", 1);
        var @object = new object();

        Assert.True(first.Equals(second));
        Assert.False(first.Equals(third));
        Assert.False(first.Equals(@object));
        Assert.True(first.Equals((object)second));
        Assert.True(first == second);
        Assert.True(first != third);
        Assert.True(first != fifth);
        Assert.False(first == fourth);
        Assert.NotEqual(first.GetHashCode(), third.GetHashCode());
    }

    [Fact]
    public void RedactorRequested_Supports_Value_Semantic_Comparisons()
    {
        var dc = new DataClassification("TAX", "1");
        var first = new RedactorRequested(dc, 0);
        var second = new RedactorRequested(dc, 0);
        var third = new RedactorRequested(dc, 1);
        var fourth = new RedactorRequested(dc, 0);
        var @object = new object();

        Assert.True(first.Equals(second));
        Assert.False(first.Equals(third));
        Assert.False(first.Equals(@object));
        Assert.True(first.Equals((object)second));
        Assert.True(first == second);
        Assert.True(first != third);
        Assert.True(first == fourth);
        Assert.NotEqual(first.GetHashCode(), third.GetHashCode());
    }
}
