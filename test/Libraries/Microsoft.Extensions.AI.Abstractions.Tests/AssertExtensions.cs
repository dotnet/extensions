// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Extensions.AI;

internal static class AssertExtensions
{
    /// <summary>
    /// Asserts that the two message lists are equal.
    /// </summary>
    public static void EqualMessageLists(List<ChatMessage> expectedMessages, List<ChatMessage> actualMessages)
    {
        Assert.Equal(expectedMessages.Count, actualMessages.Count);
        for (int i = 0; i < expectedMessages.Count; i++)
        {
            var expectedMessage = expectedMessages[i];
            var chatMessage = actualMessages[i];

            Assert.Equal(expectedMessage.Role, chatMessage.Role);
            Assert.Equal(expectedMessage.Text, chatMessage.Text);
            Assert.Equal(expectedMessage.GetType(), chatMessage.GetType());

            Assert.Equal(expectedMessage.Contents.Count, chatMessage.Contents.Count);
            for (int j = 0; j < expectedMessage.Contents.Count; j++)
            {
                var expectedItem = expectedMessage.Contents[j];
                var chatItem = chatMessage.Contents[j];

                Assert.Equal(expectedItem.GetType(), chatItem.GetType());
                Assert.Equal(expectedItem.ToString(), chatItem.ToString());
                if (expectedItem is FunctionCallContent expectedFunctionCall)
                {
                    var chatFunctionCall = (FunctionCallContent)chatItem;
                    Assert.Equal(expectedFunctionCall.Name, chatFunctionCall.Name);
                    AssertExtensions.EqualFunctionCallParameters(expectedFunctionCall.Arguments, chatFunctionCall.Arguments);
                }
                else if (expectedItem is FunctionResultContent expectedFunctionResult)
                {
                    var chatFunctionResult = (FunctionResultContent)chatItem;
                    AssertExtensions.EqualFunctionCallResults(expectedFunctionResult.Result, chatFunctionResult.Result);
                }
            }
        }
    }

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

    /// <summary>
    /// Asserts that the two JSON values are equal.
    /// </summary>
    public static void EqualJsonValues(JsonElement expectedJson, JsonElement actualJson, string? propertyName = null)
    {
        if (!JsonNode.DeepEquals(
            JsonSerializer.SerializeToNode(expectedJson, AIJsonUtilities.DefaultOptions),
            JsonSerializer.SerializeToNode(actualJson, AIJsonUtilities.DefaultOptions)))
        {
            string message = propertyName is null
                ? $"JSON result does not match expected JSON.\r\nExpected: {expectedJson.GetRawText()}\r\nActual:   {actualJson.GetRawText()}"
                : $"Parameter '{propertyName}' does not match expected JSON.\r\nExpected: {expectedJson.GetRawText()}\r\nActual:   {actualJson.GetRawText()}";

            throw new XunitException(message);
        }
    }

    private static void AreJsonEquivalentValues(object? expected, object? actual, JsonSerializerOptions? options, string? propertyName = null)
    {
        options ??= AIJsonUtilities.DefaultOptions;
        JsonElement expectedElement = NormalizeToElement(expected, options);
        JsonElement actualElement = NormalizeToElement(actual, options);
        EqualJsonValues(expectedElement, actualElement, propertyName);

        static JsonElement NormalizeToElement(object? value, JsonSerializerOptions options)
            => value is JsonElement e ? e : JsonSerializer.SerializeToElement(value, options);
    }
}
