// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatClientStructuredOutputExtensionsTests
{
    [Fact]
    public async Task SuccessUsage()
    {
        var expectedResult = new Animal { Id = 1, FullName = "Tigger", Species = Species.Tiger };
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(expectedResult)))
        {
            ResponseId = "test",
            CreatedAt = DateTimeOffset.UtcNow,
            ModelId = "someModel",
            RawRepresentation = new object(),
            Usage = new(),
        };

        using var client = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                var responseFormat = Assert.IsType<ChatResponseFormatJson>(options!.ResponseFormat);
                Assert.Null(responseFormat.Schema);
                Assert.Null(responseFormat.SchemaName);
                Assert.Null(responseFormat.SchemaDescription);

                // The inner client receives a trailing "user" message with the schema instruction
                Assert.Collection(messages,
                    message => Assert.Equal("Hello", message.Text),
                    message =>
                    {
                        Assert.Equal(ChatRole.User, message.Role);
                        Assert.Contains("Respond with a JSON value", message.Text);
                        Assert.Contains("https://json-schema.org/draft/2020-12/schema", message.Text);
                        foreach (Species v in Enum.GetValues(typeof(Species)))
                        {
                            Assert.Contains(v.ToString(), message.Text); // All enum values are described as strings
                        }
                    });

                return Task.FromResult(expectedResponse);
            },
        };

        var chatHistory = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var response = await client.GetResponseAsync<Animal>(chatHistory);

        // The response contains the deserialized result and other response properties
        Assert.Equal(1, response.Result.Id);
        Assert.Equal("Tigger", response.Result.FullName);
        Assert.Equal(Species.Tiger, response.Result.Species);
        Assert.Equal(expectedResponse.ResponseId, response.ResponseId);
        Assert.Equal(expectedResponse.CreatedAt, response.CreatedAt);
        Assert.Equal(expectedResponse.ModelId, response.ModelId);
        Assert.Same(expectedResponse.RawRepresentation, response.RawRepresentation);
        Assert.Same(expectedResponse.Usage, response.Usage);

        // TryGetResult returns the same value
        Assert.True(response.TryGetResult(out var tryGetResultOutput));
        Assert.Same(response.Result, tryGetResultOutput);

        // Doesn't mutate history (or at least, reverts any changes)
        Assert.Equal("Hello", Assert.Single(chatHistory).Text);
    }

    [Fact]
    public async Task WrapsNonObjectValuesInDataProperty()
    {
        var expectedResult = new { data = 123 };
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(expectedResult)));

        using var client = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                var suppliedSchemaMatch = Regex.Match(messages[1].Text!, "```(.*?)```", RegexOptions.Singleline);
                Assert.True(suppliedSchemaMatch.Success);
                Assert.Equal("""
                    {
                      "$schema": "https://json-schema.org/draft/2020-12/schema",
                      "type": "object",
                      "properties": {
                        "data": {
                          "$schema": "https://json-schema.org/draft/2020-12/schema",
                          "type": "integer"
                        }
                      },
                      "additionalProperties": false
                    }
                    """, suppliedSchemaMatch.Groups[1].Value.Trim());
                return Task.FromResult(expectedResponse);
            },
        };

        var response = await client.GetResponseAsync<int>("Hello");
        Assert.Equal(123, response.Result);
    }

    [Fact]
    public async Task FailureUsage_InvalidJson()
    {
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "This is not valid JSON"));
        using var client = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) => Task.FromResult(expectedResponse),
        };

        var chatHistory = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var response = await client.GetResponseAsync<Animal>(chatHistory);

        var ex = Assert.Throws<JsonException>(() => response.Result);
        Assert.Contains("invalid", ex.Message);

        Assert.False(response.TryGetResult(out var tryGetResult));
        Assert.Null(tryGetResult);
    }

    [Fact]
    public async Task FailureUsage_NullJson()
    {
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "null"));
        using var client = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) => Task.FromResult(expectedResponse),
        };

        var chatHistory = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var response = await client.GetResponseAsync<Animal>(chatHistory);

        var ex = Assert.Throws<InvalidOperationException>(() => response.Result);
        Assert.Equal("The deserialized response is null", ex.Message);

        Assert.False(response.TryGetResult(out var tryGetResult));
        Assert.Null(tryGetResult);
    }

    [Fact]
    public async Task FailureUsage_NoJsonInResponse()
    {
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, [new DataContent("https://example.com")]));
        using var client = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) => Task.FromResult(expectedResponse),
        };

        var chatHistory = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var response = await client.GetResponseAsync<Animal>(chatHistory);

        var ex = Assert.Throws<InvalidOperationException>(() => response.Result);
        Assert.Equal("The response did not contain text to be deserialized", ex.Message);

        Assert.False(response.TryGetResult(out var tryGetResult));
        Assert.Null(tryGetResult);
    }

    [Fact]
    public async Task CanUseNativeStructuredOutput()
    {
        var expectedResult = new Animal { Id = 1, FullName = "Tigger", Species = Species.Tiger };
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(expectedResult)));

        using var client = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                var responseFormat = Assert.IsType<ChatResponseFormatJson>(options!.ResponseFormat);
                Assert.Equal(nameof(Animal), responseFormat.SchemaName);
                Assert.Equal("Some test description", responseFormat.SchemaDescription);

                var responseFormatJsonSchema = JsonSerializer.Serialize(responseFormat.Schema, TestJsonSerializerContext.Default.JsonElement);
                Assert.Contains("https://json-schema.org/draft/2020-12/schema", responseFormatJsonSchema);
                foreach (Species v in Enum.GetValues(typeof(Species)))
                {
                    Assert.Contains(v.ToString(), responseFormatJsonSchema); // All enum values are described as strings
                }

                // The chat history isn't mutated any further, since native structured output is used instead of a prompt
                Assert.Equal("Hello", Assert.Single(messages).Text);

                return Task.FromResult(expectedResponse);
            },
        };

        var chatHistory = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var response = await client.GetResponseAsync<Animal>(chatHistory, useNativeJsonSchema: true);

        // The response contains the deserialized result and other response properties
        Assert.Equal(1, response.Result.Id);
        Assert.Equal("Tigger", response.Result.FullName);
        Assert.Equal(Species.Tiger, response.Result.Species);

        // TryGetResult returns the same value
        Assert.True(response.TryGetResult(out var tryGetResultOutput));
        Assert.Same(response.Result, tryGetResultOutput);

        // History remains unmutated
        Assert.Equal("Hello", Assert.Single(chatHistory).Text);
    }

    [Fact]
    public async Task CanUseNativeStructuredOutputWithSanitizedTypeName()
    {
        var expectedResult = new Data<Animal> { Value = new Animal { Id = 1, FullName = "Tigger", Species = Species.Tiger } };
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(expectedResult)));

        using var client = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                var responseFormat = Assert.IsType<ChatResponseFormatJson>(options!.ResponseFormat);

                Assert.Matches("Data_1", responseFormat.SchemaName);

                return Task.FromResult(expectedResponse);
            },
        };

        var chatHistory = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var response = await client.GetResponseAsync<Data<Animal>>(chatHistory, useNativeJsonSchema: true);

        // The response contains the deserialized result and other response properties
        Assert.Equal(1, response.Result!.Value!.Id);
        Assert.Equal("Tigger", response.Result.Value.FullName);
        Assert.Equal(Species.Tiger, response.Result.Value.Species);

        // TryGetResult returns the same value
        Assert.True(response.TryGetResult(out var tryGetResultOutput));
        Assert.Same(response.Result, tryGetResultOutput);

        // History remains unmutated
        Assert.Equal("Hello", Assert.Single(chatHistory).Text);
    }

    [Fact]
    public async Task CanUseNativeStructuredOutputWithArray()
    {
        var expectedResult = new[] { new Animal { Id = 1, FullName = "Tigger", Species = Species.Tiger } };
        var payload = new { data = expectedResult };
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(payload)));

        using var client = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) => Task.FromResult(expectedResponse)
        };

        var chatHistory = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var response = await client.GetResponseAsync<Animal[]>(chatHistory, useNativeJsonSchema: true);

        // The response contains the deserialized result and other response properties
        Assert.Single(response.Result!);
        Assert.Equal("Tigger", response.Result[0].FullName);
        Assert.Equal(Species.Tiger, response.Result[0].Species);

        // TryGetResult returns the same value
        Assert.True(response.TryGetResult(out var tryGetResultOutput));
        Assert.Same(response.Result, tryGetResultOutput);

        // History remains unmutated
        Assert.Equal("Hello", Assert.Single(chatHistory).Text);
    }

    [Fact]
    public async Task CanSpecifyCustomJsonSerializationOptions()
    {
        var jso = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
        var expectedResult = new Animal { Id = 1, FullName = "Tigger", Species = Species.Tiger };
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(expectedResult, jso)));

        using var client = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                Assert.Collection(messages,
                    message => Assert.Equal("Hello", message.Text),
                    message =>
                    {
                        Assert.Equal(ChatRole.User, message.Role);
                        Assert.Contains("Respond with a JSON value", message.Text);
                        Assert.Contains("https://json-schema.org/draft/2020-12/schema", message.Text);
                        Assert.DoesNotContain(nameof(Animal.FullName), message.Text); // The JSO uses snake_case
                        Assert.Contains("full_name", message.Text); // The JSO uses snake_case
                        Assert.DoesNotContain(nameof(Species.Tiger), message.Text); // The JSO doesn't use enum-to-string conversion
                    });

                return Task.FromResult(expectedResponse);
            },
        };

        var chatHistory = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var response = await client.GetResponseAsync<Animal>(chatHistory, jso);

        // The response contains the deserialized result and other response properties
        Assert.Equal(1, response.Result.Id);
        Assert.Equal("Tigger", response.Result.FullName);
        Assert.Equal(Species.Tiger, response.Result.Species);
    }

    [Fact]
    public async Task HandlesBackendReturningMultipleObjects()
    {
        // A very common failure mode for GPT 3.5 Turbo is that instead of returning a single top-level JSON object,
        // it may return multiple, particularly when function calling is involved.
        // See https://community.openai.com/t/2-json-objects-returned-when-using-function-calling-and-json-mode/574348
        // Fortunately we can work around this without breaking any cases of valid output.

        var expectedResult = new Animal { Id = 1, FullName = "Tigger", Species = Species.Tiger };
        var resultDuplicatedJson = JsonSerializer.Serialize(expectedResult) + Environment.NewLine + JsonSerializer.Serialize(expectedResult);

        using var client = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, resultDuplicatedJson)));
            },
        };

        var chatHistory = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var response = await client.GetResponseAsync<Animal>(chatHistory);

        // The response contains the deserialized result and other response properties
        Assert.Equal(1, response.Result.Id);
        Assert.Equal("Tigger", response.Result.FullName);
        Assert.Equal(Species.Tiger, response.Result.Species);
    }

    [Description("Some test description")]
    private class Animal
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public Species Species { get; set; }
    }

    private class Data<T>
    {
        public T? Value { get; set; }
    }

    private enum Species
    {
        Bear,
        Tiger,
        Walrus,
    }
}
