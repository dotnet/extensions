﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OllamaChatClientIntegrationTests : ChatClientIntegrationTests
{
    protected override IChatClient? CreateChatClient() =>
        IntegrationTestHelpers.GetOllamaUri() is Uri endpoint ?
            new OllamaChatClient(endpoint, "llama3.1") :
            null;

    public override Task FunctionInvocation_AutomaticallyInvokeFunction_WithParameters_Streaming() =>
        throw new SkipTestException("Ollama does not currently support function invocation with streaming.");

    public override Task Logging_LogsFunctionCalls_Streaming() =>
        throw new SkipTestException("Ollama does not currently support function invocation with streaming.");

    public override Task FunctionInvocation_RequireAny() =>
        throw new SkipTestException("Ollama does not currently support requiring function invocation.");

    public override Task FunctionInvocation_RequireSpecific() =>
        throw new SkipTestException("Ollama does not currently support requiring function invocation.");

    protected override string? GetModel_MultiModal_DescribeImage() => "llava";

    [ConditionalFact]
    public async Task PromptBasedFunctionCalling_NoArgs()
    {
        SkipIfNotEnabled();

        using var chatClient = CreateChatClient()!
            .AsBuilder()
            .UseFunctionInvocation()
            .UsePromptBasedFunctionCalling()
            .Use(innerClient => new AssertNoToolsDefinedChatClient(innerClient))
            .Build();

        var secretNumber = 42;
        var response = await chatClient.CompleteAsync("What is the current secret number? Answer with digits only.", new ChatOptions
        {
            ModelId = "llama3:8b",
            Tools = [AIFunctionFactory.Create(() => secretNumber, "GetSecretNumber")],
            Temperature = 0,
            Seed = 0,
        });

        Assert.Single(response.Choices);
        Assert.Contains(secretNumber.ToString(), response.Message.Text);
    }

    [ConditionalFact]
    public async Task PromptBasedFunctionCalling_WithArgs()
    {
        SkipIfNotEnabled();

        using var chatClient = CreateChatClient()!
            .AsBuilder()
            .UseFunctionInvocation()
            .UsePromptBasedFunctionCalling()
            .Use(innerClient => new AssertNoToolsDefinedChatClient(innerClient))
            .Build();

        var stockPriceTool = AIFunctionFactory.Create([Description("Returns the stock price for a given ticker symbol")] (
            [Description("The ticker symbol")] string symbol,
            [Description("The currency code such as USD or JPY")] string currency) =>
            {
                Assert.Equal("MSFT", symbol);
                Assert.Equal("GBP", currency);
                return 999;
            }, "GetStockPrice");

        var didCallIrrelevantTool = false;
        var irrelevantTool = AIFunctionFactory.Create(() => { didCallIrrelevantTool = true; return 123; }, "GetSecretNumber");

        var response = await chatClient.CompleteAsync("What's the stock price for Microsoft in British pounds?", new ChatOptions
        {
            Tools = [stockPriceTool, irrelevantTool],
            Temperature = 0,
            Seed = 0,
        });

        Assert.Single(response.Choices);
        Assert.Contains("999", response.Message.Text);
        Assert.False(didCallIrrelevantTool);
    }

    private sealed class AssertNoToolsDefinedChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
    {
        public override Task<ChatCompletion> CompleteAsync(
            IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            Assert.Null(options?.Tools);
            return base.CompleteAsync(chatMessages, options, cancellationToken);
        }
    }
}
