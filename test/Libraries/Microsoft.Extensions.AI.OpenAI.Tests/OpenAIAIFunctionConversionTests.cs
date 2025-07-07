// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Text.Json;
using OpenAI.Assistants;
using OpenAI.Chat;
using OpenAI.Realtime;
using OpenAI.Responses;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenAIAIFunctionConversionTests
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
}
