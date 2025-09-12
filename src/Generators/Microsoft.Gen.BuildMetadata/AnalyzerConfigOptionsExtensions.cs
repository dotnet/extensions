// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Gen.BuildMetadata;

/// <summary>
/// Extension methods for <see cref="AnalyzerConfigOptions"/> to simplify MSBuild property access.
/// </summary>
internal static class AnalyzerConfigOptionsExtensions
{
    private const string PropertyPrefix = "build_property.";

    /// <summary>
    /// Gets a boolean property value from MSBuild properties.
    /// Supports "true"/"false" values (case-insensitive).
    /// </summary>
    /// <param name="options">The analyzer configuration options.</param>
    /// <param name="propertyName">The property name (without "build_property." prefix).</param>
    /// <returns>True if the property value is "true", false otherwise.</returns>
    public static bool GetBooleanProperty(this AnalyzerConfigOptions options, string propertyName)
    {
        var value = GetProperty(options, propertyName);
        return !string.IsNullOrEmpty(value) &&
               string.Equals(value, "true", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a string property value from MSBuild properties.
    /// </summary>
    /// <param name="options">The analyzer configuration options.</param>
    /// <param name="propertyName">The property name (without "build_property." prefix).</param>
    /// <returns>The property value, or null if not found.</returns>
    public static string? GetProperty(this AnalyzerConfigOptions options, string propertyName)
    {
        var key = string.Concat(PropertyPrefix, propertyName);
        return options.TryGetValue(key, out var value) ? value : null;
    }
}
