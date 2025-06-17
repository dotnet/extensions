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
        AssertDefaults(options);
        AssertDefaults(options.Clone());
    }

    private static void AssertDefaults(ChatOptions options)
    {
        Assert.Null(options.AdditionalProperties);
        Assert.Null(options.AllowMultipleToolCalls);
        Assert.Null(options.ConversationId);
        Assert.Null(options.FrequencyPenalty);
        Assert.Null(options.Instructions);
        Assert.Null(options.MaxOutputTokens);
        Assert.Null(options.ModelId);
        Assert.Null(options.PresencePenalty);
        Assert.Null(options.ResponseFormat);
        Assert.Null(options.Temperature);
        Assert.Null(options.RawRepresentationFactory);
        Assert.Null(options.Seed);
        Assert.Null(options.StopSequences);
        Assert.Null(options.TopK);
        Assert.Null(options.TopP);
        Assert.Null(options.ToolMode);
        Assert.Null(options.Tools);
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
    public void Merge_MembersCopiedOver_Default()
    {
        using TestChatClient cc1 = new();
        using TestChatClient cc2 = new();
        using TestChatClient cc3 = new();

        ChatOptions options = new();
        AssertDefaults(options);

        options.Merge(null);
        AssertDefaults(options);

        options.Merge(new ChatOptions());
        AssertDefaults(options);

        options.Merge(new()
        {
            AdditionalProperties = new() { ["key"] = "value" },
            AllowMultipleToolCalls = true,
            ConversationId = "12345",
            FrequencyPenalty = 0.1f,
            Instructions = "Some instructions",
            MaxOutputTokens = 10,
            ModelId = "modelId",
            PresencePenalty = 0.2f,
            RawRepresentationFactory = c => c == cc1 ? new FormatException() : null,
            ResponseFormat = ChatResponseFormat.Json,
            Seed = 12345,
            StopSequences = ["stop1", "stop2"],
            Temperature = 0.3f,
            ToolMode = ChatToolMode.RequireAny,
            TopK = 5,
            TopP = 0.4f,
            Tools = [AIFunctionFactory.Create(() => 42), AIFunctionFactory.Create(() => 43)],
        });

        Assert.NotNull(options.AdditionalProperties);
        Assert.Single(options.AdditionalProperties);
        Assert.Equal("value", options.AdditionalProperties["key"]);
        Assert.IsType<FormatException>(options.RawRepresentationFactory?.Invoke(cc1));
        Assert.Null(options.RawRepresentationFactory?.Invoke(cc2));
        Assert.Null(options.RawRepresentationFactory?.Invoke(cc3));

        Assert.True(options.AllowMultipleToolCalls);
        Assert.Equal("12345", options.ConversationId);
        Assert.Equal(0.1f, options.FrequencyPenalty);
        Assert.Equal("Some instructions", options.Instructions);
        Assert.Equal(10, options.MaxOutputTokens);
        Assert.Equal("modelId", options.ModelId);
        Assert.Equal(0.2f, options.PresencePenalty);
        Assert.NotNull(options.RawRepresentationFactory);
        Assert.Same(ChatResponseFormat.Json, options.ResponseFormat);
        Assert.Equal(12345, options.Seed);
        Assert.Equal(["stop1", "stop2"], options.StopSequences);
        Assert.Equal(0.3f, options.Temperature);
        Assert.Same(ChatToolMode.RequireAny, options.ToolMode);
        Assert.Equal(5, options.TopK);
        Assert.Equal(0.4f, options.TopP);
        Assert.NotNull(options.Tools);
        Assert.Equal(2, options.Tools.Count);

        options.Merge(new()
        {
            AdditionalProperties = new() { ["key"] = "changedvalue", ["key2"] = "value2" },
            Instructions = "Updated instructions",
            MaxOutputTokens = 42,
            RawRepresentationFactory = c => c == cc2 ? new ArgumentException() : null,
            Tools = [AIFunctionFactory.Create(() => 44)],
        });

        Assert.Equal("Some instructions", options.Instructions);
        Assert.Equal(10, options.MaxOutputTokens);
        Assert.NotNull(options.AdditionalProperties);
        Assert.Equal(2, options.AdditionalProperties.Count);
        Assert.Equal("value", options.AdditionalProperties["key"]);
        Assert.Equal("value2", options.AdditionalProperties["key2"]);
        Assert.NotNull(options.Tools);
        Assert.Equal(3, options.Tools.Count);
        Assert.IsType<FormatException>(options.RawRepresentationFactory?.Invoke(cc1));
        Assert.IsType<ArgumentException>(options.RawRepresentationFactory?.Invoke(cc2));
        Assert.Null(options.RawRepresentationFactory?.Invoke(cc3));
    }

    [Fact]
    public void Merge_MembersCopiedOver_Overwrite()
    {
        using TestChatClient cc1 = new();
        using TestChatClient cc2 = new();
        using TestChatClient cc3 = new();

        ChatOptions options = new();
        AssertDefaults(options);

        options.Merge(null, overwrite: true);
        AssertDefaults(options);

        options.Merge(new ChatOptions(), overwrite: true);
        AssertDefaults(options);

        options.Merge(new()
        {
            AdditionalProperties = new() { ["key"] = "value" },
            AllowMultipleToolCalls = true,
            ConversationId = "12345",
            FrequencyPenalty = 0.1f,
            Instructions = "Some instructions",
            MaxOutputTokens = 10,
            ModelId = "modelId",
            PresencePenalty = 0.2f,
            RawRepresentationFactory = c => c == cc1 ? new FormatException() : null,
            ResponseFormat = ChatResponseFormat.Json,
            Seed = 12345,
            StopSequences = ["stop1", "stop2"],
            Temperature = 0.3f,
            ToolMode = ChatToolMode.RequireAny,
            TopK = 5,
            TopP = 0.4f,
            Tools = [AIFunctionFactory.Create(() => 42), AIFunctionFactory.Create(() => 43)],
        }, overwrite: true);

        Assert.NotNull(options.AdditionalProperties);
        Assert.Single(options.AdditionalProperties);
        Assert.Equal("value", options.AdditionalProperties["key"]);
        Assert.IsType<FormatException>(options.RawRepresentationFactory?.Invoke(cc1));
        Assert.Null(options.RawRepresentationFactory?.Invoke(cc2));
        Assert.Null(options.RawRepresentationFactory?.Invoke(cc3));

        Assert.True(options.AllowMultipleToolCalls);
        Assert.Equal("12345", options.ConversationId);
        Assert.Equal(0.1f, options.FrequencyPenalty);
        Assert.Equal("Some instructions", options.Instructions);
        Assert.Equal(10, options.MaxOutputTokens);
        Assert.Equal("modelId", options.ModelId);
        Assert.Equal(0.2f, options.PresencePenalty);
        Assert.NotNull(options.RawRepresentationFactory);
        Assert.Same(ChatResponseFormat.Json, options.ResponseFormat);
        Assert.Equal(12345, options.Seed);
        Assert.Equal(["stop1", "stop2"], options.StopSequences);
        Assert.Equal(0.3f, options.Temperature);
        Assert.Same(ChatToolMode.RequireAny, options.ToolMode);
        Assert.Equal(5, options.TopK);
        Assert.Equal(0.4f, options.TopP);
        Assert.NotNull(options.Tools);
        Assert.Equal(2, options.Tools.Count);

        options.Merge(new()
        {
            AdditionalProperties = new() { ["key"] = "changedvalue", ["key2"] = "value2" },
            Instructions = "Updated instructions",
            MaxOutputTokens = 42,
            RawRepresentationFactory = c => c == cc2 ? new ArgumentException() : null,
            Tools = [AIFunctionFactory.Create(() => 44)],
        }, overwrite: true);

        Assert.Equal("Updated instructions", options.Instructions);
        Assert.Equal(42, options.MaxOutputTokens);
        Assert.NotNull(options.AdditionalProperties);
        Assert.Equal(2, options.AdditionalProperties.Count);
        Assert.Equal("changedvalue", options.AdditionalProperties["key"]);
        Assert.Equal("value2", options.AdditionalProperties["key2"]);
        Assert.NotNull(options.Tools);
        Assert.Single(options.Tools);
        Assert.Null(options.RawRepresentationFactory?.Invoke(cc1));
        Assert.IsType<ArgumentException>(options.RawRepresentationFactory?.Invoke(cc2));
        Assert.Null(options.RawRepresentationFactory?.Invoke(cc3));
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
