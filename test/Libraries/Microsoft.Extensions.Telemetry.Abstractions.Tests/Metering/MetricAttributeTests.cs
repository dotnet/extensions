// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Telemetry.Metering.Test;

public class MetricAttributeTests
{
    private const string MyMetric = "MyMetric";

    [Fact]
    public void TestCounterAttribute()
    {
        var attribute = new CounterAttribute("d1", "d2", "d3");
        Assert.NotNull(attribute);
        Assert.Null(attribute.Name);
        Assert.Null(attribute.Type);
        Assert.Equal(new[] { "d1", "d2", "d3" }, attribute.TagNames);

        attribute.Name = MyMetric;
        Assert.Equal(MyMetric, attribute.Name);
    }

    [Fact]
    public void TestHistogramAttribute()
    {
        var attribute = new HistogramAttribute("d1", "d2", "d3");
        Assert.NotNull(attribute);
        Assert.Null(attribute.Name);
        Assert.Null(attribute.Type);
        Assert.Equal(new[] { "d1", "d2", "d3" }, attribute.TagNames);

        attribute.Name = MyMetric;
        Assert.Equal(MyMetric, attribute.Name);
    }

    [Fact]
    public void TestCounterAttributeT()
    {
        var attribute = new CounterAttribute<int>("d1", "d2", "d3");
        Assert.NotNull(attribute);
        Assert.Null(attribute.Name);
        Assert.Null(attribute.Type);
        Assert.Equal(new[] { "d1", "d2", "d3" }, attribute.TagNames);

        attribute.Name = MyMetric;
        Assert.Equal(MyMetric, attribute.Name);
    }

    [Fact]
    public void TestHistogramAttributeT()
    {
        var attribute = new HistogramAttribute<int>("d1", "d2", "d3");
        Assert.NotNull(attribute);
        Assert.Null(attribute.Name);
        Assert.Null(attribute.Type);
        Assert.Equal(new[] { "d1", "d2", "d3" }, attribute.TagNames);

        attribute.Name = MyMetric;
        Assert.Equal(MyMetric, attribute.Name);
    }

    [Fact]
    public void TestGaugeAttribute()
    {
        var attribute = new GaugeAttribute("d1", "d2", "d3");
        Assert.NotNull(attribute);
        Assert.Null(attribute.Name);
        Assert.Null(attribute.Type);
        Assert.Equal(new[] { "d1", "d2", "d3" }, attribute.TagNames);

        attribute.Name = MyMetric;
        Assert.Equal(MyMetric, attribute.Name);
    }

    [Fact]
    public void TestStrongTypeCounterAttribute()
    {
        var attribute = new CounterAttribute(typeof(TagNameTest));

        Assert.NotNull(attribute);
        Assert.Null(attribute.Name);
        Assert.Null(attribute.TagNames);
        Assert.Equal(typeof(TagNameTest), attribute.Type);

        attribute.Name = MyMetric;
        Assert.Equal(MyMetric, attribute.Name);
    }

    [Fact]
    public void TestStrongTypeHistogramAttribute()
    {
        var attribute = new HistogramAttribute(typeof(TagNameTest));

        Assert.NotNull(attribute);
        Assert.Null(attribute.Name);
        Assert.Null(attribute.TagNames);
        Assert.Equal(typeof(TagNameTest), attribute.Type);

        attribute.Name = MyMetric;
        Assert.Equal(MyMetric, attribute.Name);
    }

    [Fact]
    public void TestStrongTypeCounterAttributeT()
    {
        var attribute = new CounterAttribute<byte>(typeof(TagNameTest));

        Assert.NotNull(attribute);
        Assert.Null(attribute.Name);
        Assert.Null(attribute.TagNames);
        Assert.Equal(typeof(TagNameTest), attribute.Type);

        attribute.Name = MyMetric;
        Assert.Equal(MyMetric, attribute.Name);
    }

    [Fact]
    public void TestStrongTypeHistogramAttributeT()
    {
        var attribute = new HistogramAttribute<byte>(typeof(TagNameTest));

        Assert.NotNull(attribute);
        Assert.Null(attribute.Name);
        Assert.Null(attribute.TagNames);
        Assert.Equal(typeof(TagNameTest), attribute.Type);

        attribute.Name = MyMetric;
        Assert.Equal(MyMetric, attribute.Name);
    }

    [Fact]
    public void TestStrongTypeGaugeAttribute()
    {
        var attribute = new GaugeAttribute(typeof(TagNameTest));

        Assert.NotNull(attribute);
        Assert.Null(attribute.Name);
        Assert.Null(attribute.TagNames);
        Assert.Equal(typeof(TagNameTest), attribute.Type);

        attribute.Name = MyMetric;
        Assert.Equal(MyMetric, attribute.Name);
    }

    [Fact]
    public void TestTagNameAttribute()
    {
        var attribute = new TagNameAttribute("testName");

        Assert.NotNull(attribute);
        Assert.Equal("testName", attribute.Name);
    }

    public class TagNameTest
    {
        public string? D1 { get; set; }
        public string? D2 { get; set; }
        public string? D3 { get; set; }
    }
}
