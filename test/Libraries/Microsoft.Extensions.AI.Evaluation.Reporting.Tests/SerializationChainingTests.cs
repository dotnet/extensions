// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Tests;

/// <summary>
/// These tests are designed to verify that the M.E.AI.Evaluation.Reporting library can serialize and deserialize
/// types that it doesn't know about, but that M.E.AI does know about. For example JsonElement.
/// </summary>
public class SerializationChainingTests
{
    private ScenarioRunResult _scenarioRunResult =
        new(scenarioName: "ScenarioName",
            iterationName: "IterationName",
            executionName: "ExecutionName",
            creationTime: DateTime.UtcNow,
            messages: [],
            modelResponse: new ChatResponse
            {
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Role = ChatRole.User,
                        Contents = new List<AIContent>
                        {
                            new TextContent("A user message"),
                        },
                    },
                },
                AdditionalProperties = new AdditionalPropertiesDictionary
                {
                    { "model", "gpt-7" },
                    { "data", JsonDocument.Parse("{\"some\":\"data\"}").RootElement },
                },
            }, evaluationResult: new EvaluationResult()
        );

    private static void VerifyScenarioRunResult(ScenarioRunResult? resp)
    {
        Assert.NotNull(resp);
        Assert.Single(resp.ModelResponse.Messages);
        Assert.Equal(ChatRole.User, resp.ModelResponse.Messages[0].Role);
        Assert.Equal("A user message", resp.ModelResponse.Messages[0].Text);
        Assert.NotNull(resp.ModelResponse.AdditionalProperties);
        Assert.Equal("gpt-7", resp.ModelResponse.AdditionalProperties?["model"]?.ToString());

        string jsonFromElement = resp.ModelResponse.AdditionalProperties?["data"]?.ToString()!;
        jsonFromElement = Regex.Replace(jsonFromElement, @"\s+", "");
        Assert.Equal("{\"some\":\"data\"}", jsonFromElement);
    }

    [Fact]
    public void SerializeScenarioResult_JsonUtilities_Default()
    {
        string text = JsonSerializer.Serialize(_scenarioRunResult, JsonUtilities.Default.ScenarioRunResultTypeInfo);
        ScenarioRunResult? response = JsonSerializer.Deserialize(text, JsonUtilities.Default.ScenarioRunResultTypeInfo);

        VerifyScenarioRunResult(response);
    }

    [Fact]
    public void SerializeRoundTripChatResponse_JsonUtilities_Compact()
    {
        string text = JsonSerializer.Serialize(_scenarioRunResult, JsonUtilities.Compact.ScenarioRunResultTypeInfo);
        ScenarioRunResult? response = JsonSerializer.Deserialize(text, JsonUtilities.Compact.ScenarioRunResultTypeInfo);

        VerifyScenarioRunResult(response);
    }

    [Fact]
    public void SerializeRoundTripChatResponse_AzureStorageJsonUtilities_Default()
    {
        string text = JsonSerializer.Serialize(_scenarioRunResult, AzureStorageJsonUtilities.Default.ScenarioRunResultTypeInfo);
        ScenarioRunResult? response = JsonSerializer.Deserialize(text, AzureStorageJsonUtilities.Default.ScenarioRunResultTypeInfo);

        VerifyScenarioRunResult(response);
    }

    [Fact]
    public void SerializeRoundTripChatResponse_AzureStorageJsonUtilities_Compact()
    {
        string text = JsonSerializer.Serialize(_scenarioRunResult, AzureStorageJsonUtilities.Compact.ScenarioRunResultTypeInfo);
        ScenarioRunResult? response = JsonSerializer.Deserialize(text, AzureStorageJsonUtilities.Compact.ScenarioRunResultTypeInfo);

        VerifyScenarioRunResult(response);
    }
}
