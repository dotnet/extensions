// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test;

public class HttpClientLoggingDimensionsTest
{
    [Fact]
    public void GetDimensionNames_ReturnsAnArrayOfDimensionNames()
    {
        var actualDimensions = HttpClientLoggingTagNames.DimensionNames;
        var expectedDimensions = GetStringConstants(typeof(HttpClientLoggingTagNames));

        actualDimensions.Should().BeEquivalentTo(expectedDimensions);
    }

    private static string[] GetStringConstants(IReflect type)
    {
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

        return fields
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToArray();
    }
}
