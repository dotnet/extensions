// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using OpenAI.Assistants;
using OpenAI.Chat;
using OpenAI.Realtime;
using OpenAI.Responses;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenAIConversionTests
{
    private static readonly AIFunction _testFunction = AIFunctionFactory.Create(
        ([Description("The name parameter")] string name) => name,
        "test_function",
        "A test function for conversion");

    [Fact]
    public void AsOpenAIChatTool_ProducesValidInstance()
    {
        var tool = _testFunction.AsOpenAIChatTool();

        Assert.NotNull(tool);
        Assert.Equal("test_function", tool.FunctionName);
        Assert.Equal("A test function for conversion", tool.FunctionDescription);
        ValidateSchemaParameters(tool.FunctionParameters);
    }

    [Fact]
    public void AsOpenAIResponseTool_ProducesValidInstance()
    {
        var tool = _testFunction.AsOpenAIResponseTool();

        Assert.NotNull(tool);
    }

    [Fact]
    public void AsOpenAIConversationFunctionTool_ProducesValidInstance()
    {
        var tool = _testFunction.AsOpenAIConversationFunctionTool();

        Assert.NotNull(tool);
        Assert.Equal("test_function", tool.Name);
        Assert.Equal("A test function for conversion", tool.Description);
        ValidateSchemaParameters(tool.Parameters);
    }

    [Fact]
    public void AsOpenAIAssistantsFunctionToolDefinition_ProducesValidInstance()
    {
        var tool = _testFunction.AsOpenAIAssistantsFunctionToolDefinition();

        Assert.NotNull(tool);
        Assert.Equal("test_function", tool.FunctionName);
        Assert.Equal("A test function for conversion", tool.Description);
        ValidateSchemaParameters(tool.Parameters);
    }

    /// <summary>Helper method to validate function parameters match our schema.</summary>
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
    public void AsOpenAIChatMessages_ProducesExpectedOutput()
    {
        Assert.Throws<ArgumentNullException>("messages", () => ((IEnumerable<ChatMessage>)null!).AsOpenAIChatMessages());

        List<ChatMessage> messages =
        [
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "Hello"),
            new(ChatRole.Assistant,
            [
                new TextContent("Hi there!"),
                new FunctionCallContent("callid123", "SomeFunction", new Dictionary<string, object?>
                {
                    ["param1"] = "value1",
                    ["param2"] = 42
                }),
            ]),
            new(ChatRole.Tool, [new FunctionResultContent("callid123", "theresult")]),
            new(ChatRole.Assistant, "The answer is 42."),
        ];

        var convertedMessages = messages.AsOpenAIChatMessages().ToArray();

        Assert.Equal(5, convertedMessages.Length);

        SystemChatMessage m0 = Assert.IsType<SystemChatMessage>(convertedMessages[0]);
        Assert.Equal("You are a helpful assistant.", Assert.Single(m0.Content).Text);

        UserChatMessage m1 = Assert.IsType<UserChatMessage>(convertedMessages[1]);
        Assert.Equal("Hello", Assert.Single(m1.Content).Text);

        AssistantChatMessage m2 = Assert.IsType<AssistantChatMessage>(convertedMessages[2]);
        Assert.Single(m2.Content);
        Assert.Equal("Hi there!", m2.Content[0].Text);
        var tc = Assert.Single(m2.ToolCalls);
        Assert.Equal("callid123", tc.Id);
        Assert.Equal("SomeFunction", tc.FunctionName);
        Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["param1"] = "value1",
            ["param2"] = 42
        }), JsonSerializer.Deserialize<JsonElement>(tc.FunctionArguments.ToMemory().Span)));

        ToolChatMessage m3 = Assert.IsType<ToolChatMessage>(convertedMessages[3]);
        Assert.Equal("callid123", m3.ToolCallId);
        Assert.Equal("theresult", Assert.Single(m3.Content).Text);

        AssistantChatMessage m4 = Assert.IsType<AssistantChatMessage>(convertedMessages[4]);
        Assert.Equal("The answer is 42.", Assert.Single(m4.Content).Text);
    }

    [Fact]
    public void AsOpenAIResponseItems_ProducesExpectedOutput()
    {
        Assert.Throws<ArgumentNullException>("messages", () => ((IEnumerable<ChatMessage>)null!).AsOpenAIResponseItems());

        List<ChatMessage> messages =
        [
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "Hello"),
            new(ChatRole.Assistant,
            [
                new TextContent("Hi there!"),
                new FunctionCallContent("callid123", "SomeFunction", new Dictionary<string, object?>
                {
                    ["param1"] = "value1",
                    ["param2"] = 42
                }),
            ]),
            new(ChatRole.Tool, [new FunctionResultContent("callid123", "theresult")]),
            new(ChatRole.Assistant, "The answer is 42."),
        ];

        var convertedItems = messages.AsOpenAIResponseItems().ToArray();

        Assert.Equal(6, convertedItems.Length);

        MessageResponseItem m0 = Assert.IsAssignableFrom<MessageResponseItem>(convertedItems[0]);
        Assert.Equal("You are a helpful assistant.", Assert.Single(m0.Content).Text);

        MessageResponseItem m1 = Assert.IsAssignableFrom<MessageResponseItem>(convertedItems[1]);
        Assert.Equal(OpenAI.Responses.MessageRole.User, m1.Role);
        Assert.Equal("Hello", Assert.Single(m1.Content).Text);

        MessageResponseItem m2 = Assert.IsAssignableFrom<MessageResponseItem>(convertedItems[2]);
        Assert.Equal(OpenAI.Responses.MessageRole.Assistant, m2.Role);
        Assert.Equal("Hi there!", Assert.Single(m2.Content).Text);

        FunctionCallResponseItem m3 = Assert.IsAssignableFrom<FunctionCallResponseItem>(convertedItems[3]);
        Assert.Equal("callid123", m3.CallId);
        Assert.Equal("SomeFunction", m3.FunctionName);
        Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["param1"] = "value1",
            ["param2"] = 42
        }), JsonSerializer.Deserialize<JsonElement>(m3.FunctionArguments.ToMemory().Span)));

        FunctionCallOutputResponseItem m4 = Assert.IsAssignableFrom<FunctionCallOutputResponseItem>(convertedItems[4]);
        Assert.Equal("callid123", m4.CallId);
        Assert.Equal("theresult", m4.FunctionOutput);

        MessageResponseItem m5 = Assert.IsAssignableFrom<MessageResponseItem>(convertedItems[5]);
        Assert.Equal(OpenAI.Responses.MessageRole.Assistant, m5.Role);
        Assert.Equal("The answer is 42.", Assert.Single(m5.Content).Text);
    }
}
