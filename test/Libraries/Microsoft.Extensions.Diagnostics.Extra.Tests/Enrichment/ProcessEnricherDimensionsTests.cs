﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Enrichment.Test;

public class ProcessEnricherDimensionsTests
{
    [Fact]
    public void GetDimensionNames_ReturnsAnArrayOfDimensionNames()
    {
        IReadOnlyList<string> dimensions = ProcessEnricherTagNames.DimensionNames;
        string[] expectedDimensions = GetStringConstants(typeof(ProcessEnricherTagNames));
        dimensions.Should().BeEquivalentTo(expectedDimensions);
    }

    private static string[] GetStringConstants(IReflect type)
    {
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

        return fields
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToArray();
    }
}
