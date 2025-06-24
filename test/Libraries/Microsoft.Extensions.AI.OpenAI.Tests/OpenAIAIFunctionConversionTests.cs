// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenAI.Chat;
using OpenAI.RealtimeConversation;
using OpenAI.Responses;
using Xunit;

#pragma warning disable S103 // Lines should not be too long

namespace Microsoft.Extensions.AI;

public class OpenAIAIFunctionConversionTests
{
    // Test implementation of AIFunction
    private class TestAIFunction : AIFunction
    {
        public TestAIFunction(string name, string description, Dictionary<string, object> jsonSchema)
        {
            Name = name;
            Description = description;
            JsonSchema = System.Text.Json.JsonSerializer.SerializeToElement(jsonSchema);
        }

        public override string Name { get; }
        public override string Description { get; }
        public override JsonElement JsonSchema { get; }
        protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken) => await Task.FromResult<object?>(null);
    }

    // Shared schema for all tests
    private static readonly Dictionary<string, object> _testFunctionSchema = new()
    {
        ["type"] = "object",
        ["properties"] = new Dictionary<string, object>
        {
            ["name"] = new Dictionary<string, object>
            {
                ["type"] = "string",
                ["description"] = "The name parameter"
            }
        },
        ["required"] = new[] { "name" }
    };

    // Helper method to validate function parameters match our schema
    private static void ValidateSchemaParameters(BinaryData parameters)
    {
        Assert.NotNull(parameters);

        using var jsonDoc = JsonDocument.Parse(parameters);
        var root = jsonDoc.RootElement;

        Assert.Equal("object", root.GetProperty("type").GetString());
        Assert.True(root.TryGetProperty("properties", out var properties));
        Assert.True(properties.TryGetProperty("name", out var nameProperty));
        Assert.Equal("string", nameProperty.GetProperty("type").GetString());
        Assert.Equal("The name parameter", nameProperty.GetProperty("description").GetString());
    }

    [Fact]
    public void AIFunctionToChatToolConversionWorks()
    {
        AIFunction aiFunction = new TestAIFunction(
            "test_function",
            "A test function for conversion",
            _testFunctionSchema);

        ChatTool chatTool = aiFunction.AsOpenAIChatTool();

        Assert.NotNull(chatTool);
        Assert.Equal("test_function", chatTool.FunctionName);
        Assert.Equal("A test function for conversion", chatTool.FunctionDescription);
        ValidateSchemaParameters(chatTool.FunctionParameters);
    }

    [Fact]
    public void AIFunctionToResponseToolConversionWorks()
    {
        AIFunction aiFunction = new TestAIFunction(
            "test_function",
            "A test function for conversion",
            _testFunctionSchema);

        ResponseTool responseTool = aiFunction.AsOpenAIResponseTool();

        Assert.NotNull(responseTool);
    }

    [Fact]
    public void AIFunctionToConversationFunctionToolConversionWorks()
    {
        AIFunction aiFunction = new TestAIFunction(
            "test_function",
            "A test function for conversion",
            _testFunctionSchema);

        ConversationFunctionTool conversationTool = aiFunction.AsOpenAIConversationFunctionTool();

        Assert.NotNull(conversationTool);
        Assert.Equal("test_function", conversationTool.Name);
        Assert.Equal("A test function for conversion", conversationTool.Description);
        ValidateSchemaParameters(conversationTool.Parameters);
    }
}
