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

    [Fact]
    public void AsChatResponse_ConvertsOpenAIChatCompletion()
    {
        Assert.Throws<ArgumentNullException>("chatCompletion", () => ((ChatCompletion)null!).AsChatResponse());

        ChatCompletion cc = OpenAIChatModelFactory.ChatCompletion(
            "id", OpenAI.Chat.ChatFinishReason.Length, null, null,
            [ChatToolCall.CreateFunctionToolCall("id", "functionName", BinaryData.FromString("test"))],
            ChatMessageRole.User, null, null, null, new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            "model123", null, OpenAIChatModelFactory.ChatTokenUsage(2, 1, 3));
        cc.Content.Add(ChatMessageContentPart.CreateTextPart("Hello, world!"));
        cc.Content.Add(ChatMessageContentPart.CreateImagePart(new Uri("http://example.com/image.png")));

        ChatResponse response = cc.AsChatResponse();

        Assert.Equal("id", response.ResponseId);
        Assert.Equal(ChatFinishReason.Length, response.FinishReason);
        Assert.Equal("model123", response.ModelId);
        Assert.Equal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), response.CreatedAt);
        Assert.NotNull(response.Usage);
        Assert.Equal(1, response.Usage.InputTokenCount);
        Assert.Equal(2, response.Usage.OutputTokenCount);
        Assert.Equal(3, response.Usage.TotalTokenCount);

        ChatMessage message = Assert.Single(response.Messages);
        Assert.Equal(ChatRole.User, message.Role);

        Assert.Equal(3, message.Contents.Count);
        Assert.Equal("Hello, world!", Assert.IsType<TextContent>(message.Contents[0]).Text);
        Assert.Equal("http://example.com/image.png", Assert.IsType<UriContent>(message.Contents[1]).Uri.ToString());
        Assert.Equal("functionName", Assert.IsType<FunctionCallContent>(message.Contents[2]).Name);
    }

    [Fact]
    public void AsChatMessages_FromOpenAIChatMessages_ProducesExpectedOutput()
    {
        Assert.Throws<ArgumentNullException>("messages", () => ((IEnumerable<OpenAI.Chat.ChatMessage>)null!).AsChatMessages().ToArray());

        List<OpenAI.Chat.ChatMessage> openAIMessages =
        [
            new SystemChatMessage("You are a helpful assistant."),
            new UserChatMessage("Hello"),
            new AssistantChatMessage(ChatMessageContentPart.CreateTextPart("Hi there!")),
            new ToolChatMessage("call456", "Function output")
        ];

        var convertedMessages = openAIMessages.AsChatMessages().ToArray();

        Assert.Equal(4, convertedMessages.Length);

        Assert.Equal("You are a helpful assistant.", convertedMessages[0].Text);
        Assert.Equal("Hello", convertedMessages[1].Text);
        Assert.Equal("Hi there!", convertedMessages[2].Text);
        Assert.Equal("Function output", convertedMessages[3].Contents.OfType<FunctionResultContent>().First().Result);
    }

    [Fact]
    public void AsChatMessages_FromResponseItems_WithNullArgument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("items", () => ((IEnumerable<ResponseItem>)null!).AsChatMessages());
    }

    [Fact]
    public void AsChatMessages_FromResponseItems_ProducesExpectedOutput()
    {
        List<ChatMessage> inputMessages =
        [
            new(ChatRole.Assistant, "Hi there!")
        ];

        var responseItems = inputMessages.AsOpenAIResponseItems().ToArray();

        var convertedMessages = responseItems.AsChatMessages().ToArray();

        Assert.Single(convertedMessages);

        var message = convertedMessages[0];
        Assert.Equal(ChatRole.Assistant, message.Role);
        Assert.Equal("Hi there!", message.Text);
    }

    [Fact]
    public void AsChatMessages_FromResponseItems_WithEmptyCollection_ReturnsEmptyCollection()
    {
        var convertedMessages = Array.Empty<ResponseItem>().AsChatMessages().ToArray();
        Assert.Empty(convertedMessages);
    }

    [Fact]
    public void AsChatMessages_FromResponseItems_WithFunctionCall_HandlesCorrectly()
    {
        List<ChatMessage> inputMessages =
        [
            new(ChatRole.Assistant,
            [
                new TextContent("I'll call a function."),
                new FunctionCallContent("call123", "TestFunction", new Dictionary<string, object?> { ["param"] = "value" })
            ])
        ];

        var responseItems = inputMessages.AsOpenAIResponseItems().ToArray();
        var convertedMessages = responseItems.AsChatMessages().ToArray();

        Assert.Single(convertedMessages);

        var message = convertedMessages[0];
        Assert.Equal(ChatRole.Assistant, message.Role);

        var textContent = message.Contents.OfType<TextContent>().FirstOrDefault();
        var functionCall = message.Contents.OfType<FunctionCallContent>().FirstOrDefault();

        Assert.NotNull(textContent);
        Assert.Equal("I'll call a function.", textContent.Text);

        Assert.NotNull(functionCall);
        Assert.Equal("call123", functionCall.CallId);
        Assert.Equal("TestFunction", functionCall.Name);
        Assert.Equal("value", functionCall.Arguments!["param"]?.ToString());
    }

    [Fact]
    public void AsOpenAIChatCompletion_WithNullArgument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("response", () => ((ChatResponse)null!).AsOpenAIChatCompletion());
    }

    [Fact]
    public void AsOpenAIChatCompletion_WithMultipleContents_ProducesValidInstance()
    {
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant,
        [
            new TextContent("Here's an image and some text."),
            new UriContent("https://example.com/image.jpg", "image/jpeg"),
            new DataContent(new byte[] { 1, 2, 3, 4 }, "application/octet-stream")
        ]))
        {
            ResponseId = "multi-content-response",
            ModelId = "gpt-4-vision",
            FinishReason = ChatFinishReason.Stop,
            CreatedAt = new DateTimeOffset(2025, 1, 3, 14, 30, 0, TimeSpan.Zero),
            Usage = new UsageDetails
            {
                InputTokenCount = 25,
                OutputTokenCount = 12,
                TotalTokenCount = 37
            }
        };

        ChatCompletion completion = chatResponse.AsOpenAIChatCompletion();

        Assert.Equal("multi-content-response", completion.Id);
        Assert.Equal("gpt-4-vision", completion.Model);
        Assert.Equal(OpenAI.Chat.ChatFinishReason.Stop, completion.FinishReason);
        Assert.Equal(ChatMessageRole.Assistant, completion.Role);
        Assert.Equal(new DateTimeOffset(2025, 1, 3, 14, 30, 0, TimeSpan.Zero), completion.CreatedAt);

        Assert.NotNull(completion.Usage);
        Assert.Equal(25, completion.Usage.InputTokenCount);
        Assert.Equal(12, completion.Usage.OutputTokenCount);
        Assert.Equal(37, completion.Usage.TotalTokenCount);

        Assert.NotEmpty(completion.Content);
        Assert.Contains(completion.Content, c => c.Text == "Here's an image and some text.");
    }

    [Fact]
    public void AsOpenAIChatCompletion_WithEmptyData_HandlesGracefully()
    {
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello"));
        var completion = chatResponse.AsOpenAIChatCompletion();

        Assert.NotNull(completion);
        Assert.Equal(ChatMessageRole.Assistant, completion.Role);
        Assert.Equal("Hello", Assert.Single(completion.Content).Text);
        Assert.Empty(completion.ToolCalls);

        var emptyResponse = new ChatResponse([]);
        var emptyCompletion = emptyResponse.AsOpenAIChatCompletion();
        Assert.NotNull(emptyCompletion);
        Assert.Equal(ChatMessageRole.Assistant, emptyCompletion.Role);
    }

    [Fact]
    public void AsOpenAIChatCompletion_WithComplexFunctionCallArguments_SerializesCorrectly()
    {
        var complexArgs = new Dictionary<string, object?>
        {
            ["simpleString"] = "hello",
            ["number"] = 42,
            ["boolean"] = true,
            ["nullValue"] = null,
            ["nestedObject"] = new Dictionary<string, object?>
            {
                ["innerString"] = "world",
                ["innerArray"] = new[] { 1, 2, 3 }
            }
        };

        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant,
        [
            new TextContent("I'll process this complex data."),
            new FunctionCallContent("process_data", "ProcessComplexData", complexArgs)
        ]))
        {
            ResponseId = "complex-function-call",
            ModelId = "gpt-4",
            FinishReason = ChatFinishReason.ToolCalls
        };

        ChatCompletion completion = chatResponse.AsOpenAIChatCompletion();

        Assert.Equal("complex-function-call", completion.Id);
        Assert.Equal(OpenAI.Chat.ChatFinishReason.ToolCalls, completion.FinishReason);

        var toolCall = Assert.Single(completion.ToolCalls);
        Assert.Equal("process_data", toolCall.Id);
        Assert.Equal("ProcessComplexData", toolCall.FunctionName);

        var deserializedArgs = JsonSerializer.Deserialize<Dictionary<string, object?>>(toolCall.FunctionArguments.ToMemory().Span);
        Assert.NotNull(deserializedArgs);
        Assert.Equal("hello", deserializedArgs["simpleString"]?.ToString());
        Assert.Equal(42, ((JsonElement)deserializedArgs["number"]!).GetInt32());
        Assert.True(((JsonElement)deserializedArgs["boolean"]!).GetBoolean());
        Assert.Null(deserializedArgs["nullValue"]);

        var nestedObj = (JsonElement)deserializedArgs["nestedObject"]!;
        Assert.Equal("world", nestedObj.GetProperty("innerString").GetString());
        Assert.Equal(3, nestedObj.GetProperty("innerArray").GetArrayLength());
    }

    [Fact]
    public void AsOpenAIChatCompletion_WithDifferentFinishReasons_MapsCorrectly()
    {
        var testCases = new[]
        {
            (ChatFinishReason.Stop, OpenAI.Chat.ChatFinishReason.Stop),
            (ChatFinishReason.Length, OpenAI.Chat.ChatFinishReason.Length),
            (ChatFinishReason.ContentFilter, OpenAI.Chat.ChatFinishReason.ContentFilter),
            (ChatFinishReason.ToolCalls, OpenAI.Chat.ChatFinishReason.ToolCalls)
        };

        foreach (var (inputFinishReason, expectedOpenAIFinishReason) in testCases)
        {
            var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Test"))
            {
                FinishReason = inputFinishReason
            };

            var completion = chatResponse.AsOpenAIChatCompletion();
            Assert.Equal(expectedOpenAIFinishReason, completion.FinishReason);
        }
    }

    [Fact]
    public void AsOpenAIChatCompletion_WithDifferentRoles_MapsCorrectly()
    {
        var testCases = new[]
        {
            (ChatRole.Assistant, ChatMessageRole.Assistant),
            (ChatRole.User, ChatMessageRole.User),
            (ChatRole.System, ChatMessageRole.System),
            (ChatRole.Tool, ChatMessageRole.Tool)
        };

        foreach (var (inputRole, expectedOpenAIRole) in testCases)
        {
            var chatResponse = new ChatResponse(new ChatMessage(inputRole, "Test"));
            var completion = chatResponse.AsOpenAIChatCompletion();
            Assert.Equal(expectedOpenAIRole, completion.Role);
        }
    }
}
