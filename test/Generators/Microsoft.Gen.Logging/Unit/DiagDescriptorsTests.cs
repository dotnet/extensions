// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.Logging.Parsing;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class DiagDescriptorsTests
{
    public static IEnumerable<object?[]> DiagDescriptorsData()
    {
        var type = typeof(DiagDescriptors);
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty))
        {
            var value = property.GetValue(type, null);
            yield return new[] { value };
        }
    }

    [Theory]
    [MemberData(nameof(DiagDescriptorsData))]
    public void ShouldContainValidLinkAndBeEnabled(DiagnosticDescriptor descriptor)
    {
        Assert.True(descriptor.IsEnabledByDefault, descriptor.Id + " should be enabled by default");
        Assert.EndsWith("/" + descriptor.Id, descriptor.HelpLinkUri, StringComparison.OrdinalIgnoreCase);
    }
}
