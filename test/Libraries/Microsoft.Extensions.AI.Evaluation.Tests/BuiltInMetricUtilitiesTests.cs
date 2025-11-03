// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias Evaluation;
using Evaluation::Microsoft.Extensions.AI.Evaluation;
using Evaluation::Microsoft.Extensions.AI.Evaluation.Utilities;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Tests;

public class BuiltInMetricUtilitiesTests
{
    [Fact]
    public void MarkAsBuiltInAddsMetadata()
    {
        var metric = new NumericMetric("name");
        metric.MarkAsBuiltIn();
        Assert.True(metric.IsBuiltIn());
    }

    [Fact]
    public void IsBuiltInReturnsFalseIfMetadataIsMissing()
    {
        var metric = new NumericMetric("name");
        Assert.False(metric.IsBuiltIn());
    }

    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    [InlineData("True")]
    public void MetadataValueOfTrueIsCaseInsensitive(string value)
    {
        var metric = new BooleanMetric("name");
        metric.AddOrUpdateMetadata(BuiltInMetricUtilities.BuiltInEvalMetadataName, value);
        Assert.True(metric.IsBuiltIn());
    }

    [Theory]
    [InlineData("false")]
    [InlineData("FALSE")]
    [InlineData("False")]
    public void MetadataValueOfFalseIsCaseInsensitive(string value)
    {
        var metric = new StringMetric("name");
        metric.AddOrUpdateMetadata(BuiltInMetricUtilities.BuiltInEvalMetadataName, value);
        Assert.False(metric.IsBuiltIn());
    }

    [Fact]
    public void UnrecognizedMetadataValueIsTreatedAsFalse()
    {
        var metric = new NumericMetric("name");
        metric.AddOrUpdateMetadata(BuiltInMetricUtilities.BuiltInEvalMetadataName, "unrecognized");
        Assert.False(metric.IsBuiltIn());
    }

    [Fact]
    public void EmptyMetadataValueIsTreatedAsFalse()
    {
        var metric = new NumericMetric("name");
        metric.AddOrUpdateMetadata(BuiltInMetricUtilities.BuiltInEvalMetadataName, string.Empty);
        Assert.False(metric.IsBuiltIn());
    }
}
