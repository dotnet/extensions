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
        yield return new object[] { "Invalid", DataClassification.Unknown };
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
           { "Invalid", DataClassification.Unknown },
        };

        // Assert
        options.Value.Example.Should().NotBeNull().And.Be(new DataClassification("Example", "Test"));
        options.Value.Facts.Should().NotBeEmpty().And.Equal(expected);
    }

    [Fact]
    public void CanConvertTo_ShouldReturnTrue_WhenDestinationTypeIsDataClassification()
    {
        // Arrange
        var converter = new DataClassificationTypeConverter();

        // Act
        var canConvert = converter.CanConvertTo(null, typeof(DataClassification));

        // Assert
        canConvert.Should().BeTrue();
    }

    [Fact]
    public void CanConvertTo_ShouldReturnFalse_WhenDestinationTypeIsNotDataClassification()
    {
        // Arrange
        var converter = new DataClassificationTypeConverter();

        // Act
        var canConvert = converter.CanConvertTo(null, typeof(DateTimeOffset));

        // Assert
        canConvert.Should().BeFalse();
    }

    [Fact]
    public void ConvertFrom_ShouldReturnDataClassificationUnknown_WhenValueIsNotString()
    {
        // Arrange
        var converter = new DataClassificationTypeConverter();

        // Act
        var result = converter.ConvertFrom(null, null, 123);

        // Assert
        result.Should().Be(DataClassification.Unknown);
    }

    [Fact]
    public void CanConvertFrom_ShouldReturnTrue_WhenSourceTypeIsString()
    {
        // Arrange
        var converter = new DataClassificationTypeConverter();

        // Act
        var canConvert = converter.CanConvertFrom(null, typeof(string));

        // Assert
        canConvert.Should().BeTrue();
    }

    [Fact]
    public void CanConvertFrom_ShouldReturnFalse_WhenSourceTypeIsNotString()
    {
        // Arrange
        var converter = new DataClassificationTypeConverter();

        // Act
        var canConvert = converter.CanConvertFrom(null, typeof(int));

        // Assert
        canConvert.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(DefaultDataClassificationTestData))]
    [MemberData(nameof(CustomDataClassificationTestData))]
    public void ConvertFrom_ShouldReturnCorrectDataClassification_WhenValueIsValidString(string input, DataClassification expected)
    {
        // Arrange
        var converter = new DataClassificationTypeConverter();

        // Act
        var result = converter.ConvertFrom(null, null, input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertFrom_ShouldReturnUnknown_WhenInputIsInvalidFormat()
    {
        // Arrange
        var converter = new DataClassificationTypeConverter();
        string input = "InvalidFormatWithoutDelimiter";

        // Act
        var result = converter.ConvertFrom(null, null, input);

        // Assert
        result.Should().Be(DataClassification.Unknown);
    }

    [Theory]
    [InlineData("None")]
    [InlineData("ValidString")]
    public void IsValid_ShouldReturnTrue_ForValidStringWithoutDelimiter(string input)
    {
        // Arrange
        var converter = new DataClassificationTypeConverter();

        // Act
        var isValid = converter.IsValid(null, input);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_ForValidStringWithDelimiter()
    {
        // Arrange
        var converter = new DataClassificationTypeConverter();
        string input = "Taxonomy:Value";

        // Act
        var isValid = converter.IsValid(null, input);

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(":", false)]
    [InlineData("A:", false)]
    [InlineData(":A", false)]
    public void IsValid_ShouldReturnExpectedResult_ForInsufficientLengthInput(string input, bool expected)
    {
        // Arrange  
        var converter = new DataClassificationTypeConverter();

        // Act  
        var isValid = converter.IsValid(null, input);

        // Assert  
        isValid.Should().Be(expected);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_ForNonStringInput()
    {
        // Arrange
        var converter = new DataClassificationTypeConverter();
        int input = 456;

        // Act
        var isValid = converter.IsValid(null, input);

        // Assert
        isValid.Should().BeFalse();
    }

    private class TestOptions
    {
#pragma warning disable S3459 // Unassigned members should be removed
        public DataClassification? Example { get; set; }
#pragma warning restore S3459 // Unassigned members should be removed
        public IDictionary<string, DataClassification> Facts { get; set; } = new Dictionary<string, DataClassification>();
    }
}
