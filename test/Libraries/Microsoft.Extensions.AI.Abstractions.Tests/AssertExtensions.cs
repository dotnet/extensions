// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Extensions.AI;

internal static class AssertExtensions
{
    /// <summary>
    /// Asserts that the two function call parameters are equal, up to JSON equivalence.
    /// </summary>
    public static void EqualFunctionCallParameters(
        IDictionary<string, object?>? expected,
        IDictionary<string, object?>? actual,
        JsonSerializerOptions? options = null)
    {
        if (expected is null || actual is null)
        {
            Assert.Equal(expected, actual);
            return;
        }

        foreach (var expectedEntry in expected)
        {
            if (!actual.TryGetValue(expectedEntry.Key, out object? actualValue))
            {
                throw new XunitException($"Expected parameter '{expectedEntry.Key}' not found in actual value.");
            }

            AreJsonEquivalentValues(expectedEntry.Value, actualValue, options, propertyName: expectedEntry.Key);
        }

        if (expected.Count != actual.Count)
        {
            var extraParameters = actual
                .Where(e => !expected.ContainsKey(e.Key))
                .Select(e => $"'{e.Key}'")
                .First();

            throw new XunitException($"Actual value contains additional parameters {string.Join(", ", extraParameters)} not found in expected value.");
        }
    }

    /// <summary>
    /// Asserts that the two function call results are equal, up to JSON equivalence.
    /// </summary>
    public static void EqualFunctionCallResults(object? expected, object? actual, JsonSerializerOptions? options = null)
        => AreJsonEquivalentValues(expected, actual, options);

    private static void AreJsonEquivalentValues(object? expected, object? actual, JsonSerializerOptions? options, string? propertyName = null)
    {
        options ??= JsonSerializerOptions.Default;
        JsonElement expectedElement = NormalizeToElement(expected, options);
        JsonElement actualElement = NormalizeToElement(actual, options);
        if (!JsonElement.DeepEquals(expectedElement, actualElement))
        {
            string message = propertyName is null
                ? $"Function result does not match expected JSON.\r\nExpected: {expectedElement.GetRawText()}\r\nActual:   {actualElement.GetRawText()}"
                : $"Parameter '{propertyName}' does not match expected JSON.\r\nExpected: {expectedElement.GetRawText()}\r\nActual:   {actualElement.GetRawText()}";

            throw new XunitException(message);
        }

        static JsonElement NormalizeToElement(object? value, JsonSerializerOptions options)
            => value is JsonElement e ? e : JsonSerializer.SerializeToElement(value, options);
    }
}
