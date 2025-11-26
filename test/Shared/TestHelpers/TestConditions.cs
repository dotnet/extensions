// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace Microsoft.Extensions.TestHelpers;

/// <summary>
/// Provides static helper methods for conditional test execution with Microsoft.DotNet.XUnitExtensions.
/// </summary>
public static class TestConditions
{
    /// <summary>
    /// Checks if the specified environment variable has one of the specified values.
    /// </summary>
    /// <param name="variableName">The name of the environment variable.</param>
    /// <param name="values">The values to check against.</param>
    /// <returns>True if the environment variable matches one of the values; otherwise, false.</returns>
    public static bool IsEnvironmentVariableSet(string variableName, params string[] values)
    {
        var currentValue = Environment.GetEnvironmentVariable(variableName);
        return values.Any(value => string.Equals(value, currentValue, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if AI_TEMPLATES_TEST_PROJECT_NAMES environment variable is set to "true" or "1".
    /// </summary>
    public static bool IsAITemplatesTestProjectNamesSet =>
        IsEnvironmentVariableSet("AI_TEMPLATES_TEST_PROJECT_NAMES", "true", "1");
}
