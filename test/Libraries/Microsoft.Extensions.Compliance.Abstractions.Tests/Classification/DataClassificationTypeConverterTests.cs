// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Compliance.Classification.Tests;

public class DataClassificationTypeConverterTests
{
    public static IEnumerable<object[]> DefaultDataClassificationTestData()
    {
        yield return new object[] { "None", DataClassification.None };
        yield return new object[] { "Unknown", DataClassification.Unknown };
    }

    public static IEnumerable<object[]> CustomDataClassificationTestData()
    {
        yield return new object[] { "Example:Test", new DataClassification("Example", "Test") };
        yield return new object[] { "Taxonomy:Value", new DataClassification("Taxonomy", "Value") };
        yield return new object[] { "Custom:Data", new DataClassification("Custom", "Data") };
    }

    [Fact]
    public void BindServiceCollection_ShouldReturnIOptionsWithExpectedDataClassification()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Key:Example", "Example:Test"),
                new KeyValuePair<string, string?>("Key:Facts:Value", "Taxonomy:Value"),
                new KeyValuePair<string, string?>("Key:Facts:Data", "Custom:Data"),
                new KeyValuePair<string, string?>("Key:Facts:None", "None"),
                new KeyValuePair<string, string?>("Key:Facts:Unknown", "Unknown"),
                new KeyValuePair<string, string?>("Key:Facts:Invalid", "Invalid"),
            })
            .Build();

        IServiceCollection serviceCollection = new ServiceCollection()
            .Configure<TestOptions>(configuration.GetSection("Key"));

        // Act
        using var sp = serviceCollection.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<TestOptions>>();

        var expected = new Dictionary<string, DataClassification>
        {
           { "Value", new DataClassification("Taxonomy", "Value") },
           { "Data", new DataClassification("Custom", "Data") },
           { "None", DataClassification.None },
           { "Unknown", DataClassification.Unknown },
        };

        // Assert
        options.Value.Example.Should().NotBeNull().And.Be(new DataClassification("Example", "Test"));
        options.Value.Facts.Should().NotBeEmpty().And.Equal(expected);

        // Odd quirk: binding to dictionary succeeds but doesn't include invalid values
        options.Value.Facts.Should().NotContainKey("Invalid");
    }

    [Theory]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(DataClassification), false)]
    public void CanConvertFrom_ShouldReturnExpectedResult(Type sourceType, bool expected)
    {
        // Arrange
        var converter = new DataClassificationTypeConverter();

        // Act
        var result = converter.CanConvertFrom(null, sourceType);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(typeof(DataClassification), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(string), false)]
    public void CanConvertTo_ShouldReturnExpectedResult(Type destinationType, bool expected)
    {
        // Arrange
        var converter = new DataClassificationTypeConverter();

        // Act
        var result = converter.CanConvertTo(null, destinationType);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("None", "", "None")]
    [InlineData("Unknown", "", "Unknown")]
    [InlineData("Example:Test", "Example", "Test")]
    public void ConvertFrom_ShouldReturnExpectedResult_ForValidInput(string input, string expectedTaxonomyName, string expectedValue)
    {
        // Arrange
        var converter = new DataClassificationTypeConverter();

        // Act
        var result = converter.ConvertFrom(null, null, input);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<DataClassification>();

#pragma warning disable CS8605 // Unboxing a possibly null value.
        var dataClassification = (DataClassification)result;
#pragma warning restore CS8605 // Unboxing a possibly null value.

        dataClassification.TaxonomyName.Should().Be(expectedTaxonomyName);
        dataClassification.Value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("InvalidFormat", typeof(FormatException))]
    [InlineData("InvalidFormat:", typeof(FormatException))]
    [InlineData(":InvalidFormat", typeof(FormatException))]
    [InlineData(":", typeof(FormatException))]
    [InlineData("", typeof(FormatException))]
    [InlineData(42, typeof(ArgumentException))]
    [InlineData(false, typeof(ArgumentException))]
    public void ConvertFrom_ShouldThrowException_ForInvalidInput(object input, Type expectedException)
    {
        // Arrange
        var converter = new DataClassificationTypeConverter();

        // Act
        var act = () => converter.ConvertFrom(null, null, input);

        // Assert
        Assert.Throws(expectedException, act);
    }

    [Theory]
    [InlineData("None", true)]
    [InlineData("Unknown", true)]
    [InlineData("Example:Test", true)]
    [InlineData("InvalidFormat", false)]
    [InlineData("InvalidFormat:", false)]
    [InlineData(":InvalidFormat", false)]
    [InlineData(":", false)]
    [InlineData("", false)]
    [InlineData(42, false)]
    [InlineData(false, false)]
    public void IsValid_ShouldReturnExpectedResult(object input, bool expected)
    {
        // Arrange
        var converter = new DataClassificationTypeConverter();

        // Act
        var result = converter.IsValid(null, input);

        // Assert
        result.Should().Be(expected);
    }

    private class TestOptions
    {
#pragma warning disable S3459 // Unassigned members should be removed
#pragma warning disable S1144 // Unused private types or members should be removed
        public DataClassification? Example { get; set; }
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore S3459 // Unassigned members should be removed
        public IDictionary<string, DataClassification> Facts { get; set; } = new Dictionary<string, DataClassification>();
    }
}
