// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    public void AsOpenAIChatResponseFormat_HandlesVariousFormats()
    {
        Assert.Null(MicrosoftExtensionsAIChatExtensions.AsOpenAIChatResponseFormat(null));

        var text = MicrosoftExtensionsAIChatExtensions.AsOpenAIChatResponseFormat(ChatResponseFormat.Text);
        Assert.NotNull(text);
        Assert.Equal("""{"type":"text"}""", ((IJsonModel<OpenAI.Chat.ChatResponseFormat>)text).Write(ModelReaderWriterOptions.Json).ToString());

        var json = MicrosoftExtensionsAIChatExtensions.AsOpenAIChatResponseFormat(ChatResponseFormat.Json);
        Assert.NotNull(json);
        Assert.Equal("""{"type":"json_object"}""", ((IJsonModel<OpenAI.Chat.ChatResponseFormat>)json).Write(ModelReaderWriterOptions.Json).ToString());

        var jsonSchema = ChatResponseFormat.ForJsonSchema(typeof(int), schemaName: "my_schema", schemaDescription: "A test schema").AsOpenAIChatResponseFormat();
        Assert.NotNull(jsonSchema);
        Assert.Equal(RemoveWhitespace("""
            {"type":"json_schema","json_schema":{"description":"A test schema","name":"my_schema","schema":{
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "integer"
            }}}
            """), RemoveWhitespace(((IJsonModel<OpenAI.Chat.ChatResponseFormat>)jsonSchema).Write(ModelReaderWriterOptions.Json).ToString()));

        jsonSchema = ChatResponseFormat.ForJsonSchema(typeof(int), schemaName: "my_schema", schemaDescription: "A test schema").AsOpenAIChatResponseFormat(
            new() { AdditionalProperties = new AdditionalPropertiesDictionary { ["strictJsonSchema"] = true } });
        Assert.NotNull(jsonSchema);
        Assert.Equal(RemoveWhitespace("""
            {
            "type":"json_schema","json_schema":{"description":"A test schema","name":"my_schema","schema":{
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "integer"
            },"strict":true}}
            """), RemoveWhitespace(((IJsonModel<OpenAI.Chat.ChatResponseFormat>)jsonSchema).Write(ModelReaderWriterOptions.Json).ToString()));
    }

    [Fact]
    public void AsOpenAIResponseTextFormat_HandlesVariousFormats()
    {
        Assert.Null(MicrosoftExtensionsAIResponsesExtensions.AsOpenAIResponseTextFormat(null));

        var text = MicrosoftExtensionsAIResponsesExtensions.AsOpenAIResponseTextFormat(ChatResponseFormat.Text);
        Assert.NotNull(text);
        Assert.Equal(ResponseTextFormatKind.Text, text.Kind);

        var json = MicrosoftExtensionsAIResponsesExtensions.AsOpenAIResponseTextFormat(ChatResponseFormat.Json);
        Assert.NotNull(json);
        Assert.Equal(ResponseTextFormatKind.JsonObject, json.Kind);

        var jsonSchema = ChatResponseFormat.ForJsonSchema(typeof(int), schemaName: "my_schema", schemaDescription: "A test schema").AsOpenAIResponseTextFormat();
        Assert.NotNull(jsonSchema);
        Assert.Equal(ResponseTextFormatKind.JsonSchema, jsonSchema.Kind);
        Assert.Equal(RemoveWhitespace("""
            {"type":"json_schema","description":"A test schema","name":"my_schema","schema":{
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "integer"
            }}
            """), RemoveWhitespace(((IJsonModel<ResponseTextFormat>)jsonSchema).Write(ModelReaderWriterOptions.Json).ToString()));

        jsonSchema = ChatResponseFormat.ForJsonSchema(typeof(int), schemaName: "my_schema", schemaDescription: "A test schema").AsOpenAIResponseTextFormat(
            new() { AdditionalProperties = new AdditionalPropertiesDictionary { ["strictJsonSchema"] = true } });
        Assert.NotNull(jsonSchema);
        Assert.Equal(ResponseTextFormatKind.JsonSchema, jsonSchema.Kind);
        Assert.Equal(RemoveWhitespace("""
            {"type":"json_schema","description":"A test schema","name":"my_schema","schema":{
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "integer"
            },"strict":true}
            """), RemoveWhitespace(((IJsonModel<ResponseTextFormat>)jsonSchema).Write(ModelReaderWriterOptions.Json).ToString()));
    }

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AsOpenAIChatMessages_ProducesExpectedOutput(bool withOptions)
    {
        Assert.Throws<ArgumentNullException>("messages", () => ((IEnumerable<ChatMessage>)null!).AsOpenAIChatMessages());

        List<ChatMessage> messages =
        [
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "Hello") { AuthorName = "Jane" },
            new(ChatRole.Assistant,
            [
                new TextContent("Hi there!"),
                new FunctionCallContent("callid123", "SomeFunction", new Dictionary<string, object?>
                {
                    ["param1"] = "value1",
                    ["param2"] = 42
                }),
            ]) { AuthorName = "!@#$%John Smith^*)" },
            new(ChatRole.Tool, [new FunctionResultContent("callid123", "theresult")]),
            new(ChatRole.Assistant, "The answer is 42.") { AuthorName = "@#$#$@$" },
        ];

        ChatOptions? options = withOptions ? new ChatOptions { Instructions = "You talk like a parrot." } : null;

        var convertedMessages = messages.AsOpenAIChatMessages(options).ToArray();

        int index = 0;
        if (withOptions)
        {
            Assert.Equal(6, convertedMessages.Length);

            index = 1;
            SystemChatMessage instructionsMessage = Assert.IsType<SystemChatMessage>(convertedMessages[0], exactMatch: false);
            Assert.Equal("You talk like a parrot.", Assert.Single(instructionsMessage.Content).Text);
        }
        else
        {
            Assert.Equal(5, convertedMessages.Length);
        }

        SystemChatMessage m0 = Assert.IsType<SystemChatMessage>(convertedMessages[index], exactMatch: false);
        Assert.Equal("You are a helpful assistant.", Assert.Single(m0.Content).Text);

        UserChatMessage m1 = Assert.IsType<UserChatMessage>(convertedMessages[index + 1], exactMatch: false);
        Assert.Equal("Hello", Assert.Single(m1.Content).Text);
        Assert.Equal("Jane", m1.ParticipantName);

        AssistantChatMessage m2 = Assert.IsType<AssistantChatMessage>(convertedMessages[index + 2], exactMatch: false);
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
        Assert.Equal("JohnSmith", m2.ParticipantName);

        ToolChatMessage m3 = Assert.IsType<ToolChatMessage>(convertedMessages[index + 3], exactMatch: false);
        Assert.Equal("callid123", m3.ToolCallId);
        Assert.Equal("theresult", Assert.Single(m3.Content).Text);

        AssistantChatMessage m4 = Assert.IsType<AssistantChatMessage>(convertedMessages[index + 4], exactMatch: false);
        Assert.Equal("The answer is 42.", Assert.Single(m4.Content).Text);
        Assert.Null(m4.ParticipantName);
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
        Assert.Equal("Hello, world!", Assert.IsType<TextContent>(message.Contents[0], exactMatch: false).Text);
        Assert.Equal("http://example.com/image.png", Assert.IsType<UriContent>(message.Contents[1], exactMatch: false).Uri.ToString());
        Assert.Equal("functionName", Assert.IsType<FunctionCallContent>(message.Contents[2], exactMatch: false).Name);
    }

    [Fact]
    public async Task AsChatResponse_ConvertsOpenAIStreamingChatCompletionUpdates()
    {
        Assert.Throws<ArgumentNullException>("chatCompletionUpdates", () => ((IAsyncEnumerable<StreamingChatCompletionUpdate>)null!).AsChatResponseUpdatesAsync());

        List<ChatResponseUpdate> updates = [];
        await foreach (var update in CreateUpdates().AsChatResponseUpdatesAsync())
        {
            updates.Add(update);
        }

        var response = updates.ToChatResponse();

        Assert.Equal("id", response.ResponseId);
        Assert.Equal(ChatFinishReason.ToolCalls, response.FinishReason);
        Assert.Equal("model123", response.ModelId);
        Assert.Equal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), response.CreatedAt);
        Assert.NotNull(response.Usage);
        Assert.Equal(1, response.Usage.InputTokenCount);
        Assert.Equal(2, response.Usage.OutputTokenCount);
        Assert.Equal(3, response.Usage.TotalTokenCount);

        ChatMessage message = Assert.Single(response.Messages);
        Assert.Equal(ChatRole.Assistant, message.Role);

        Assert.Equal(3, message.Contents.Count);
        Assert.Equal("Hello, world!", Assert.IsType<TextContent>(message.Contents[0], exactMatch: false).Text);
        Assert.Equal("http://example.com/image.png", Assert.IsType<UriContent>(message.Contents[1], exactMatch: false).Uri.ToString());
        Assert.Equal("functionName", Assert.IsType<FunctionCallContent>(message.Contents[2], exactMatch: false).Name);

        static async IAsyncEnumerable<StreamingChatCompletionUpdate> CreateUpdates()
        {
            await Task.Yield();
            yield return OpenAIChatModelFactory.StreamingChatCompletionUpdate(
                "id",
                new ChatMessageContent(
                    ChatMessageContentPart.CreateTextPart("Hello, world!"),
                    ChatMessageContentPart.CreateImagePart(new Uri("http://example.com/image.png"))),
                null,
                [OpenAIChatModelFactory.StreamingChatToolCallUpdate(0, "id", ChatToolCallKind.Function, "functionName", BinaryData.FromString("test"))],
                ChatMessageRole.Assistant,
                null, null, null, OpenAI.Chat.ChatFinishReason.ToolCalls, new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                "model123", null, OpenAIChatModelFactory.ChatTokenUsage(2, 1, 3));
        }
    }

    [Fact]
    public void AsChatResponse_ConvertsOpenAIResponse()
    {
        Assert.Throws<ArgumentNullException>("response", () => ((OpenAIResponse)null!).AsChatResponse());

        // The OpenAI library currently doesn't provide any way to create an OpenAIResponse instance,
        // as all constructors/factory methods currently are internal. Update this test when such functionality is available.
    }

    [Fact]
    public void AsChatResponseUpdatesAsync_ConvertsOpenAIStreamingResponseUpdates()
    {
        Assert.Throws<ArgumentNullException>("responseUpdates", () => ((IAsyncEnumerable<StreamingResponseUpdate>)null!).AsChatResponseUpdatesAsync());

        // The OpenAI library currently doesn't provide any way to create a StreamingResponseUpdate instance,
        // as all constructors/factory methods currently are internal. Update this test when such functionality is available.
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

    [Fact]
    public async Task AsOpenAIStreamingChatCompletionUpdatesAsync_WithNullArgument_ThrowsArgumentNullException()
    {
        var asyncEnumerable = ((IAsyncEnumerable<ChatResponseUpdate>)null!).AsOpenAIStreamingChatCompletionUpdatesAsync();
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await asyncEnumerable.GetAsyncEnumerator().MoveNextAsync());
    }

    [Fact]
    public async Task AsOpenAIStreamingChatCompletionUpdatesAsync_WithEmptyCollection_ReturnsEmptySequence()
    {
        var updates = new List<ChatResponseUpdate>();
        var result = new List<StreamingChatCompletionUpdate>();

        await foreach (var update in CreateAsyncEnumerable(updates).AsOpenAIStreamingChatCompletionUpdatesAsync())
        {
            result.Add(update);
        }

        Assert.Empty(result);
    }

    [Fact]
    public async Task AsOpenAIStreamingChatCompletionUpdatesAsync_WithRawRepresentation_ReturnsOriginal()
    {
        var originalUpdate = OpenAIChatModelFactory.StreamingChatCompletionUpdate(
            "test-id",
            new ChatMessageContent(ChatMessageContentPart.CreateTextPart("Hello")),
            role: ChatMessageRole.Assistant,
            finishReason: OpenAI.Chat.ChatFinishReason.Stop,
            createdAt: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            model: "gpt-3.5-turbo");

        var responseUpdate = new ChatResponseUpdate(ChatRole.Assistant, "Hello")
        {
            RawRepresentation = originalUpdate
        };

        var result = new List<StreamingChatCompletionUpdate>();
        await foreach (var update in CreateAsyncEnumerable(new[] { responseUpdate }).AsOpenAIStreamingChatCompletionUpdatesAsync())
        {
            result.Add(update);
        }

        Assert.Single(result);
        Assert.Same(originalUpdate, result[0]);
    }

    [Fact]
    public async Task AsOpenAIStreamingChatCompletionUpdatesAsync_WithTextContent_CreatesValidUpdate()
    {
        var responseUpdate = new ChatResponseUpdate(ChatRole.Assistant, "Hello, world!")
        {
            ResponseId = "response-123",
            MessageId = "message-456",
            ModelId = "gpt-4",
            FinishReason = ChatFinishReason.Stop,
            CreatedAt = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero)
        };

        var result = new List<StreamingChatCompletionUpdate>();
        await foreach (var update in CreateAsyncEnumerable(new[] { responseUpdate }).AsOpenAIStreamingChatCompletionUpdatesAsync())
        {
            result.Add(update);
        }

        Assert.Single(result);
        var streamingUpdate = result[0];

        Assert.Equal("response-123", streamingUpdate.CompletionId);
        Assert.Equal("gpt-4", streamingUpdate.Model);
        Assert.Equal(OpenAI.Chat.ChatFinishReason.Stop, streamingUpdate.FinishReason);
        Assert.Equal(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero), streamingUpdate.CreatedAt);
        Assert.Equal(ChatMessageRole.Assistant, streamingUpdate.Role);
        Assert.Equal("Hello, world!", Assert.Single(streamingUpdate.ContentUpdate).Text);
    }

    [Fact]
    public async Task AsOpenAIStreamingChatCompletionUpdatesAsync_WithUsageContent_CreatesUpdateWithUsage()
    {
        var responseUpdate = new ChatResponseUpdate
        {
            ResponseId = "response-123",
            Contents =
            [
                new UsageContent(new UsageDetails
                {
                    InputTokenCount = 10,
                    OutputTokenCount = 20,
                    TotalTokenCount = 30
                })
            ]
        };

        var result = new List<StreamingChatCompletionUpdate>();
        await foreach (var update in CreateAsyncEnumerable(new[] { responseUpdate }).AsOpenAIStreamingChatCompletionUpdatesAsync())
        {
            result.Add(update);
        }

        Assert.Single(result);
        var streamingUpdate = result[0];

        Assert.Equal("response-123", streamingUpdate.CompletionId);
        Assert.NotNull(streamingUpdate.Usage);
        Assert.Equal(20, streamingUpdate.Usage.OutputTokenCount);
        Assert.Equal(10, streamingUpdate.Usage.InputTokenCount);
        Assert.Equal(30, streamingUpdate.Usage.TotalTokenCount);
    }

    [Fact]
    public async Task AsOpenAIStreamingChatCompletionUpdatesAsync_WithFunctionCallContent_CreatesUpdateWithToolCalls()
    {
        var functionCallContent = new FunctionCallContent("call-123", "GetWeather", new Dictionary<string, object?>
        {
            ["location"] = "Seattle",
            ["units"] = "celsius"
        });

        var responseUpdate = new ChatResponseUpdate(ChatRole.Assistant, [functionCallContent])
        {
            ResponseId = "response-123"
        };

        var result = new List<StreamingChatCompletionUpdate>();
        await foreach (var update in CreateAsyncEnumerable(new[] { responseUpdate }).AsOpenAIStreamingChatCompletionUpdatesAsync())
        {
            result.Add(update);
        }

        Assert.Single(result);
        var streamingUpdate = result[0];

        Assert.Equal("response-123", streamingUpdate.CompletionId);
        Assert.Single(streamingUpdate.ToolCallUpdates);

        var toolCallUpdate = streamingUpdate.ToolCallUpdates[0];
        Assert.Equal(0, toolCallUpdate.Index);
        Assert.Equal("call-123", toolCallUpdate.ToolCallId);
        Assert.Equal(ChatToolCallKind.Function, toolCallUpdate.Kind);
        Assert.Equal("GetWeather", toolCallUpdate.FunctionName);

        var deserializedArgs = JsonSerializer.Deserialize<Dictionary<string, object?>>(
            toolCallUpdate.FunctionArgumentsUpdate.ToMemory().Span);
        Assert.Equal("Seattle", deserializedArgs?["location"]?.ToString());
        Assert.Equal("celsius", deserializedArgs?["units"]?.ToString());
    }

    [Fact]
    public async Task AsOpenAIStreamingChatCompletionUpdatesAsync_WithMultipleFunctionCalls_CreatesCorrectIndexes()
    {
        var functionCall1 = new FunctionCallContent("call-1", "Function1", new Dictionary<string, object?> { ["param1"] = "value1" });
        var functionCall2 = new FunctionCallContent("call-2", "Function2", new Dictionary<string, object?> { ["param2"] = "value2" });

        var responseUpdate = new ChatResponseUpdate(ChatRole.Assistant, [functionCall1, functionCall2])
        {
            ResponseId = "response-123"
        };

        var result = new List<StreamingChatCompletionUpdate>();
        await foreach (var update in CreateAsyncEnumerable(new[] { responseUpdate }).AsOpenAIStreamingChatCompletionUpdatesAsync())
        {
            result.Add(update);
        }

        Assert.Single(result);
        var streamingUpdate = result[0];

        Assert.Equal(2, streamingUpdate.ToolCallUpdates.Count);

        Assert.Equal(0, streamingUpdate.ToolCallUpdates[0].Index);
        Assert.Equal("call-1", streamingUpdate.ToolCallUpdates[0].ToolCallId);
        Assert.Equal("Function1", streamingUpdate.ToolCallUpdates[0].FunctionName);

        Assert.Equal(1, streamingUpdate.ToolCallUpdates[1].Index);
        Assert.Equal("call-2", streamingUpdate.ToolCallUpdates[1].ToolCallId);
        Assert.Equal("Function2", streamingUpdate.ToolCallUpdates[1].FunctionName);
    }

    [Fact]
    public async Task AsOpenAIStreamingChatCompletionUpdatesAsync_WithMixedContent_IncludesAllContent()
    {
        var responseUpdate = new ChatResponseUpdate(ChatRole.Assistant,
        [
            new TextContent("Processing your request..."),
            new FunctionCallContent("call-123", "GetWeather", new Dictionary<string, object?> { ["location"] = "Seattle" }),
            new UsageContent(new UsageDetails { TotalTokenCount = 50 })
        ])
        {
            ResponseId = "response-123",
            ModelId = "gpt-4"
        };

        var result = new List<StreamingChatCompletionUpdate>();
        await foreach (var update in CreateAsyncEnumerable(new[] { responseUpdate }).AsOpenAIStreamingChatCompletionUpdatesAsync())
        {
            result.Add(update);
        }

        Assert.Single(result);
        var streamingUpdate = result[0];

        Assert.Equal("response-123", streamingUpdate.CompletionId);
        Assert.Equal("gpt-4", streamingUpdate.Model);

        // Should have text content
        Assert.Contains(streamingUpdate.ContentUpdate, c => c.Text == "Processing your request...");

        // Should have tool call
        Assert.Single(streamingUpdate.ToolCallUpdates);
        Assert.Equal("call-123", streamingUpdate.ToolCallUpdates[0].ToolCallId);

        // Should have usage
        Assert.NotNull(streamingUpdate.Usage);
        Assert.Equal(50, streamingUpdate.Usage.TotalTokenCount);
    }

    [Fact]
    public async Task AsOpenAIStreamingChatCompletionUpdatesAsync_WithDifferentRoles_MapsCorrectly()
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
            var responseUpdate = new ChatResponseUpdate(inputRole, "Test message");

            var result = new List<StreamingChatCompletionUpdate>();
            await foreach (var update in CreateAsyncEnumerable(new[] { responseUpdate }).AsOpenAIStreamingChatCompletionUpdatesAsync())
            {
                result.Add(update);
            }

            Assert.Single(result);
            Assert.Equal(expectedOpenAIRole, result[0].Role);
        }
    }

    [Fact]
    public async Task AsOpenAIStreamingChatCompletionUpdatesAsync_WithDifferentFinishReasons_MapsCorrectly()
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
            var responseUpdate = new ChatResponseUpdate(ChatRole.Assistant, "Test")
            {
                FinishReason = inputFinishReason
            };

            var result = new List<StreamingChatCompletionUpdate>();
            await foreach (var update in CreateAsyncEnumerable(new[] { responseUpdate }).AsOpenAIStreamingChatCompletionUpdatesAsync())
            {
                result.Add(update);
            }

            Assert.Single(result);
            Assert.Equal(expectedOpenAIFinishReason, result[0].FinishReason);
        }
    }

    [Fact]
    public async Task AsOpenAIStreamingChatCompletionUpdatesAsync_WithMultipleUpdates_ProcessesAllCorrectly()
    {
        var updates = new[]
        {
            new ChatResponseUpdate(ChatRole.Assistant, "Hello, ")
            {
                ResponseId = "response-123",
                MessageId = "message-1"

                // No FinishReason set - null
            },
            new ChatResponseUpdate(ChatRole.Assistant, "world!")
            {
                ResponseId = "response-123",
                MessageId = "message-1",
                FinishReason = ChatFinishReason.Stop
            }
        };

        var result = new List<StreamingChatCompletionUpdate>();
        await foreach (var update in CreateAsyncEnumerable(updates).AsOpenAIStreamingChatCompletionUpdatesAsync())
        {
            result.Add(update);
        }

        Assert.Equal(2, result.Count);

        Assert.Equal("response-123", result[0].CompletionId);
        Assert.Equal("Hello, ", Assert.Single(result[0].ContentUpdate).Text);

        // The ToChatFinishReason method defaults null to Stop
        Assert.Equal(OpenAI.Chat.ChatFinishReason.Stop, result[0].FinishReason);

        Assert.Equal("response-123", result[1].CompletionId);
        Assert.Equal("world!", Assert.Single(result[1].ContentUpdate).Text);
        Assert.Equal(OpenAI.Chat.ChatFinishReason.Stop, result[1].FinishReason);
    }

    [Fact]
    public void AsOpenAIResponse_WithNullArgument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("response", () => ((ChatResponse)null!).AsOpenAIResponse());
    }

    [Fact]
    public void AsOpenAIResponse_WithRawRepresentation_ReturnsOriginal()
    {
        var originalOpenAIResponse = OpenAIResponsesModelFactory.OpenAIResponse(
            "original-response-id",
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ResponseStatus.Completed,
            usage: null,
            maxOutputTokenCount: 100,
            outputItems: [],
            parallelToolCallsEnabled: false,
            model: "gpt-4",
            temperature: 0.7f,
            topP: 0.9f,
            previousResponseId: "prev-id",
            instructions: "Test instructions");

        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Test"))
        {
            RawRepresentation = originalOpenAIResponse
        };

        var result = chatResponse.AsOpenAIResponse();

        Assert.Same(originalOpenAIResponse, result);
    }

    [Fact]
    public void AsOpenAIResponse_WithBasicChatResponse_CreatesValidOpenAIResponse()
    {
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello, world!"))
        {
            ResponseId = "test-response-id",
            ModelId = "gpt-4-turbo",
            CreatedAt = new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero),
            FinishReason = ChatFinishReason.Stop
        };

        var openAIResponse = chatResponse.AsOpenAIResponse();

        Assert.NotNull(openAIResponse);
        Assert.Equal("test-response-id", openAIResponse.Id);
        Assert.Equal("gpt-4-turbo", openAIResponse.Model);
        Assert.Equal(new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero), openAIResponse.CreatedAt);
        Assert.Equal(ResponseStatus.Completed, openAIResponse.Status);
        Assert.NotNull(openAIResponse.OutputItems);
        Assert.Single(openAIResponse.OutputItems);

        var outputItem = Assert.IsAssignableFrom<MessageResponseItem>(openAIResponse.OutputItems.First());
        Assert.Equal("Hello, world!", Assert.Single(outputItem.Content).Text);
    }

    [Fact]
    public void AsOpenAIResponse_WithChatOptions_IncludesOptionsInResponse()
    {
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Test message"))
        {
            ResponseId = "options-test",
            ModelId = "gpt-3.5-turbo"
        };

        var options = new ChatOptions
        {
            MaxOutputTokens = 500,
            AllowMultipleToolCalls = true,
            ConversationId = "conversation-123",
            Instructions = "You are a helpful assistant.",
            Temperature = 0.8f,
            TopP = 0.95f,
            ModelId = "override-model"
        };

        var openAIResponse = chatResponse.AsOpenAIResponse(options);

        Assert.Equal("options-test", openAIResponse.Id);
        Assert.Equal("gpt-3.5-turbo", openAIResponse.Model);
        Assert.Equal(500, openAIResponse.MaxOutputTokenCount);
        Assert.True(openAIResponse.ParallelToolCallsEnabled);
        Assert.Equal("conversation-123", openAIResponse.PreviousResponseId);
        Assert.Equal("You are a helpful assistant.", openAIResponse.Instructions);
        Assert.Equal(0.8f, openAIResponse.Temperature);
        Assert.Equal(0.95f, openAIResponse.TopP);
    }

    [Fact]
    public void AsOpenAIResponse_WithEmptyMessages_CreatesResponseWithEmptyOutputItems()
    {
        var chatResponse = new ChatResponse([])
        {
            ResponseId = "empty-response",
            ModelId = "gpt-4"
        };

        var openAIResponse = chatResponse.AsOpenAIResponse();

        Assert.Equal("empty-response", openAIResponse.Id);
        Assert.Equal("gpt-4", openAIResponse.Model);
        Assert.Empty(openAIResponse.OutputItems);
    }

    [Fact]
    public void AsOpenAIResponse_WithMultipleMessages_ConvertsAllMessages()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.Assistant, "First message"),
            new(ChatRole.Assistant, "Second message"),
            new(ChatRole.Assistant,
            [
                new TextContent("Third message with function call"),
                new FunctionCallContent("call-123", "TestFunction", new Dictionary<string, object?> { ["param"] = "value" })
            ])
        };

        var chatResponse = new ChatResponse(messages)
        {
            ResponseId = "multi-message-response"
        };

        var openAIResponse = chatResponse.AsOpenAIResponse();

        Assert.Equal(4, openAIResponse.OutputItems.Count);

        var messageItems = openAIResponse.OutputItems.OfType<MessageResponseItem>().ToArray();
        var functionCallItems = openAIResponse.OutputItems.OfType<FunctionCallResponseItem>().ToArray();

        Assert.Equal(3, messageItems.Length);
        Assert.Single(functionCallItems);

        Assert.Equal("First message", Assert.Single(messageItems[0].Content).Text);
        Assert.Equal("Second message", Assert.Single(messageItems[1].Content).Text);
        Assert.Equal("Third message with function call", Assert.Single(messageItems[2].Content).Text);

        Assert.Equal("call-123", functionCallItems[0].CallId);
        Assert.Equal("TestFunction", functionCallItems[0].FunctionName);
    }

    [Fact]
    public void AsOpenAIResponse_WithToolMessages_ConvertsCorrectly()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.Assistant,
            [
                new TextContent("I'll call a function"),
                new FunctionCallContent("call-456", "GetWeather", new Dictionary<string, object?> { ["location"] = "Seattle" })
            ]),
            new(ChatRole.Tool, [new FunctionResultContent("call-456", "The weather is sunny")]),
            new(ChatRole.Assistant, "The weather in Seattle is sunny!")
        };

        var chatResponse = new ChatResponse(messages)
        {
            ResponseId = "tool-message-test"
        };

        var openAIResponse = chatResponse.AsOpenAIResponse();

        var outputItems = openAIResponse.OutputItems.ToArray();
        Assert.Equal(4, outputItems.Length);

        // Should have message, function call, function output, and final message
        Assert.IsType<MessageResponseItem>(outputItems[0], exactMatch: false);
        Assert.IsType<FunctionCallResponseItem>(outputItems[1], exactMatch: false);
        Assert.IsType<FunctionCallOutputResponseItem>(outputItems[2], exactMatch: false);
        Assert.IsType<MessageResponseItem>(outputItems[3], exactMatch: false);

        var functionCallOutput = (FunctionCallOutputResponseItem)outputItems[2];
        Assert.Equal("call-456", functionCallOutput.CallId);
        Assert.Equal("The weather is sunny", functionCallOutput.FunctionOutput);
    }

    [Fact]
    public void AsOpenAIResponse_WithSystemAndUserMessages_ConvertsCorrectly()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "Hello, how are you?"),
            new(ChatRole.Assistant, "I'm doing well, thank you for asking!")
        };

        var chatResponse = new ChatResponse(messages)
        {
            ResponseId = "system-user-test"
        };

        var openAIResponse = chatResponse.AsOpenAIResponse();

        var outputItems = openAIResponse.OutputItems.ToArray();
        Assert.Equal(3, outputItems.Length);

        var systemMessage = Assert.IsType<MessageResponseItem>(outputItems[0], exactMatch: false);
        var userMessage = Assert.IsType<MessageResponseItem>(outputItems[1], exactMatch: false);
        var assistantMessage = Assert.IsType<MessageResponseItem>(outputItems[2], exactMatch: false);

        Assert.Equal("You are a helpful assistant.", Assert.Single(systemMessage.Content).Text);
        Assert.Equal("Hello, how are you?", Assert.Single(userMessage.Content).Text);
        Assert.Equal("I'm doing well, thank you for asking!", Assert.Single(assistantMessage.Content).Text);
    }

    [Fact]
    public void AsOpenAIResponse_WithDefaultValues_UsesExpectedDefaults()
    {
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Default test"));

        var openAIResponse = chatResponse.AsOpenAIResponse();

        Assert.NotNull(openAIResponse);
        Assert.Equal(ResponseStatus.Completed, openAIResponse.Status);
        Assert.False(openAIResponse.ParallelToolCallsEnabled);
        Assert.Null(openAIResponse.MaxOutputTokenCount);
        Assert.Null(openAIResponse.Temperature);
        Assert.Null(openAIResponse.TopP);
        Assert.Null(openAIResponse.PreviousResponseId);
        Assert.Null(openAIResponse.Instructions);
        Assert.NotNull(openAIResponse.OutputItems);
    }

    [Fact]
    public void AsOpenAIResponse_WithOptionsButNoModelId_UsesOptionsModelId()
    {
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Model test"));

        var options = new ChatOptions
        {
            ModelId = "options-model-id"
        };

        var openAIResponse = chatResponse.AsOpenAIResponse(options);

        Assert.Equal("options-model-id", openAIResponse.Model);
    }

    [Fact]
    public void AsOpenAIResponse_WithBothModelIds_PrefersChatResponseModelId()
    {
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Model priority test"))
        {
            ModelId = "response-model-id"
        };

        var options = new ChatOptions
        {
            ModelId = "options-model-id"
        };

        var openAIResponse = chatResponse.AsOpenAIResponse(options);

        Assert.Equal("response-model-id", openAIResponse.Model);
    }

    [Fact]
    public void ListAddResponseTool_AddsToolCorrectly()
    {
        Assert.Throws<ArgumentNullException>("tools", () => ((IList<AITool>)null!).Add(ResponseTool.CreateWebSearchTool()));
        Assert.Throws<ArgumentNullException>("tool", () => new List<AITool>().Add((ResponseTool)null!));

        Assert.Throws<ArgumentNullException>("tool", () => ((ResponseTool)null!).AsAITool());

        ChatOptions options;

        options = new()
        {
            Tools = new List<AITool> { ResponseTool.CreateWebSearchTool() },
        };
        Assert.Single(options.Tools);
        Assert.NotNull(options.Tools[0]);

        options = new()
        {
            Tools = [ResponseTool.CreateWebSearchTool().AsAITool()],
        };
        Assert.Single(options.Tools);
        Assert.NotNull(options.Tools[0]);
    }

    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            await Task.Yield();
            yield return item;
        }
    }

    private static string RemoveWhitespace(string input) => Regex.Replace(input, @"\s+", "");
}
