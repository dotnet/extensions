// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatOptionsTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        ChatOptions options = new();
        Assert.Null(options.ConversationId);
        Assert.Null(options.Instructions);
        Assert.Null(options.Temperature);
        Assert.Null(options.MaxOutputTokens);
        Assert.Null(options.TopP);
        Assert.Null(options.TopK);
        Assert.Null(options.FrequencyPenalty);
        Assert.Null(options.PresencePenalty);
        Assert.Null(options.Seed);
        Assert.Null(options.ResponseFormat);
        Assert.Null(options.ModelId);
        Assert.Null(options.StopSequences);
        Assert.Null(options.AllowMultipleToolCalls);
        Assert.Null(options.ToolMode);
        Assert.Null(options.Tools);
        Assert.Null(options.AdditionalProperties);
        Assert.Null(options.RawRepresentationFactory);

        ChatOptions clone = options.Clone();
        Assert.Null(clone.ConversationId);
        Assert.Null(clone.Instructions);
        Assert.Null(clone.Temperature);
        Assert.Null(clone.MaxOutputTokens);
        Assert.Null(clone.TopP);
        Assert.Null(clone.TopK);
        Assert.Null(clone.FrequencyPenalty);
        Assert.Null(clone.PresencePenalty);
        Assert.Null(clone.Seed);
        Assert.Null(clone.ResponseFormat);
        Assert.Null(clone.ModelId);
        Assert.Null(clone.StopSequences);
        Assert.Null(clone.AllowMultipleToolCalls);
        Assert.Null(clone.ToolMode);
        Assert.Null(clone.Tools);
        Assert.Null(clone.AdditionalProperties);
        Assert.Null(clone.RawRepresentationFactory);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        ChatOptions options = new();

        List<string> stopSequences =
        [
            "stop1",
            "stop2",
        ];

        List<AITool> tools =
        [
            AIFunctionFactory.Create(() => 42),
            AIFunctionFactory.Create(() => 43),
        ];

        AdditionalPropertiesDictionary additionalProps = new()
        {
            ["key"] = "value",
        };

        Func<IChatClient, object?> rawRepresentationFactory = (c) => null;

        options.ConversationId = "12345";
        options.Instructions = "Some instructions";
        options.Temperature = 0.1f;
        options.MaxOutputTokens = 2;
        options.TopP = 0.3f;
        options.TopK = 42;
        options.FrequencyPenalty = 0.4f;
        options.PresencePenalty = 0.5f;
        options.Seed = 12345;
        options.ResponseFormat = ChatResponseFormat.Json;
        options.ModelId = "modelId";
        options.StopSequences = stopSequences;
        options.AllowMultipleToolCalls = true;
        options.ToolMode = ChatToolMode.RequireAny;
        options.Tools = tools;
        options.RawRepresentationFactory = rawRepresentationFactory;
        options.AdditionalProperties = additionalProps;

        Assert.Equal("12345", options.ConversationId);
        Assert.Equal("Some instructions", options.Instructions);
        Assert.Equal(0.1f, options.Temperature);
        Assert.Equal(2, options.MaxOutputTokens);
        Assert.Equal(0.3f, options.TopP);
        Assert.Equal(42, options.TopK);
        Assert.Equal(0.4f, options.FrequencyPenalty);
        Assert.Equal(0.5f, options.PresencePenalty);
        Assert.Equal(12345, options.Seed);
        Assert.Same(ChatResponseFormat.Json, options.ResponseFormat);
        Assert.Equal("modelId", options.ModelId);
        Assert.Same(stopSequences, options.StopSequences);
        Assert.True(options.AllowMultipleToolCalls);
        Assert.Same(ChatToolMode.RequireAny, options.ToolMode);
        Assert.Same(tools, options.Tools);
        Assert.Same(rawRepresentationFactory, options.RawRepresentationFactory);
        Assert.Same(additionalProps, options.AdditionalProperties);

        ChatOptions clone = options.Clone();
        Assert.Equal("12345", clone.ConversationId);
        Assert.Equal(0.1f, clone.Temperature);
        Assert.Equal(2, clone.MaxOutputTokens);
        Assert.Equal(0.3f, clone.TopP);
        Assert.Equal(42, clone.TopK);
        Assert.Equal(0.4f, clone.FrequencyPenalty);
        Assert.Equal(0.5f, clone.PresencePenalty);
        Assert.Equal(12345, clone.Seed);
        Assert.Same(ChatResponseFormat.Json, clone.ResponseFormat);
        Assert.Equal("modelId", clone.ModelId);
        Assert.Equal(stopSequences, clone.StopSequences);
        Assert.True(clone.AllowMultipleToolCalls);
        Assert.Same(ChatToolMode.RequireAny, clone.ToolMode);
        Assert.Equal(tools, clone.Tools);
        Assert.Same(rawRepresentationFactory, clone.RawRepresentationFactory);
        Assert.Equal(additionalProps, clone.AdditionalProperties);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        ChatOptions options = new();

        List<string> stopSequences =
        [
            "stop1",
            "stop2",
        ];

        AdditionalPropertiesDictionary additionalProps = new()
        {
            ["key"] = "value",
        };

        options.ConversationId = "12345";
        options.Instructions = "Some instructions";
        options.Temperature = 0.1f;
        options.MaxOutputTokens = 2;
        options.TopP = 0.3f;
        options.TopK = 42;
        options.FrequencyPenalty = 0.4f;
        options.PresencePenalty = 0.5f;
        options.Seed = 12345;
        options.ResponseFormat = ChatResponseFormat.Json;
        options.ModelId = "modelId";
        options.StopSequences = stopSequences;
        options.AllowMultipleToolCalls = false;
        options.ToolMode = ChatToolMode.RequireAny;
        options.Tools =
        [
            AIFunctionFactory.Create(() => 42),
            AIFunctionFactory.Create(() => 43),
        ];
        options.RawRepresentationFactory = (c) => null;
        options.AdditionalProperties = additionalProps;

        string json = JsonSerializer.Serialize(options, TestJsonSerializerContext.Default.ChatOptions);

        ChatOptions? deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ChatOptions);
        Assert.NotNull(deserialized);

        Assert.Equal("12345", deserialized.ConversationId);
        Assert.Equal("Some instructions", deserialized.Instructions);
        Assert.Equal(0.1f, deserialized.Temperature);
        Assert.Equal(2, deserialized.MaxOutputTokens);
        Assert.Equal(0.3f, deserialized.TopP);
        Assert.Equal(42, deserialized.TopK);
        Assert.Equal(0.4f, deserialized.FrequencyPenalty);
        Assert.Equal(0.5f, deserialized.PresencePenalty);
        Assert.Equal(12345, deserialized.Seed);
        Assert.IsType<ChatResponseFormatJson>(deserialized.ResponseFormat);
        Assert.Equal("modelId", deserialized.ModelId);
        Assert.NotSame(stopSequences, deserialized.StopSequences);
        Assert.Equal(stopSequences, deserialized.StopSequences);
        Assert.False(deserialized.AllowMultipleToolCalls);
        Assert.Equal(ChatToolMode.RequireAny, deserialized.ToolMode);
        Assert.Null(deserialized.Tools);
        Assert.Null(deserialized.RawRepresentationFactory);

        Assert.NotNull(deserialized.AdditionalProperties);
        Assert.Single(deserialized.AdditionalProperties);
        Assert.True(deserialized.AdditionalProperties.TryGetValue("key", out object? value));
        Assert.IsType<JsonElement>(value);
        Assert.Equal("value", ((JsonElement)value!).GetString());
    }
}
