// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Gen.Logging.Emission;
using Microsoft.Gen.Logging.Model;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class EmitterUtilsTests
{
    [Theory]
    [InlineData("\n", "\\n")]
    [InlineData("\r", "\\r")]
    [InlineData("\"", "\\\"")]
    [InlineData("\\", "\\\\")]
    [InlineData("\n\r\"", "\\n\\r\\\"")]
    [InlineData("no special chars...", "no special chars...")]
    [InlineData("special \n chars \r within \n\n a \"string\"", "special \\n chars \\r within \\n\\n a \\\"string\\\"")]
    public void ShouldEscapeMessageStringCorrectly(string input, string expected)
    {
        Assert.Equal(expected, Emitter.EscapeMessageString(input));
    }

    [Theory]
    [InlineData("\n", "\\n")]
    [InlineData("\r", "\\r")]
    [InlineData("<", "&lt;")]
    [InlineData(">", "&gt;")]
    [InlineData("no special chars...", "no special chars...")]
    [InlineData("special \n chars \r within \n\n a \"string\"", "special \\n chars \\r within \\n\\n a \"string\"")]
    public void ShouldEscapeMessageStringForXmlDocumentationCorrectly(string input, string expected)
    {
        Assert.Equal(expected, Emitter.EscapeMessageStringForXmlDocumentation(input));
    }

    [Fact]
    public void ShouldGetLogPropertiesDataClassesCorrectly()
    {
        var publicDataFullName = typeof(PublicDataAttribute).FullName!;

        var lm = new LoggingMethod();
        lm.Parameters.Add(new LoggingMethodParameter
        {
            TagProvider = new TagProvider(string.Empty, string.Empty),
            PropertiesToLog = new List<LoggingProperty> { new LoggingProperty("a", "b", publicDataFullName, false, false, false, false, false, false, Array.Empty<LoggingProperty>()) }
        });

        lm.Parameters.Add(new LoggingMethodParameter
        {
            TagProvider = null,
            PropertiesToLog = new List<LoggingProperty> { new LoggingProperty("c", "d", publicDataFullName, false, false, false, false, false, false, Array.Empty<LoggingProperty>()) }
        });

        var result = Emitter.GetLogPropertiesAttributes(lm);
        Assert.Collection(result, x => Assert.Equal(publicDataFullName, x));
    }

    [Fact]
    public void ShouldNotFindLogLevelIfNoneAvailable()
    {
        var lm = new LoggingMethod
        {
            Level = null
        };

        Assert.Empty(Emitter.GetLoggerMethodLogLevel(lm));
    }

    [Fact]
    public void ShouldFindLogLevelFromParameter()
    {
        const string ParamName = "Test name";

        var lm = new LoggingMethod
        {
            Level = null
        };

        lm.Parameters.Add(new LoggingMethodParameter
        {
            IsLogLevel = true,
            Name = ParamName
        });

        Assert.Equal(ParamName, Emitter.GetLoggerMethodLogLevel(lm));
    }
}
