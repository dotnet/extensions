// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.AI.Evaluation.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Integration.Tests;

[Experimental("AIEVAL001")]
public class AgentQualityEvaluatorTests
{
    private static readonly ChatOptions? _chatOptions;
    private static readonly ChatOptions? _chatOptionsWithTools;
    private static readonly ReportingConfiguration? _agentQualityReportingConfiguration;
    private static readonly ReportingConfiguration? _needsContextReportingConfiguration;

    static AgentQualityEvaluatorTests()
    {
        if (Settings.Current.Configured)
        {
            _chatOptions =
                new ChatOptions
                {
                    Temperature = 0.0f,
                    ResponseFormat = ChatResponseFormat.Text
                };

            _chatOptionsWithTools =
                new ChatOptions
                {
                    Temperature = 0.0f,
                    ResponseFormat = ChatResponseFormat.Text,
                    Tools =
                    [
                        AIFunctionFactory.Create(
                            GetOrders, name: $"{nameof(AgentQualityEvaluatorTests)}_{nameof(GetOrders)}"),
                        AIFunctionFactory.Create(
                            GetOrderStatus, name: $"{nameof(AgentQualityEvaluatorTests)}_{nameof(GetOrderStatus)}")
                    ]

                    // Note: We prefix the tool names with the test class name so that the tool names used are
                    // consistent between Microsoft.Extensions.AI and SemanticKernel.
                };

            ChatConfiguration chatConfiguration = Setup.CreateChatConfiguration();
            ChatClientMetadata? clientMetadata = chatConfiguration.ChatClient.GetService<ChatClientMetadata>();

            IChatClient chatClient = chatConfiguration.ChatClient;
            IChatClient chatClientWithToolCalling = chatClient.AsBuilder().UseFunctionInvocation().Build();
            ChatConfiguration chatConfigurationWithToolCalling = new ChatConfiguration(chatClientWithToolCalling);

            string version = $"Product Version: {Constants.Version}";
            string date = $"Date: {DateTime.UtcNow:dddd, dd MMMM yyyy}";
            string projectName = $"Project: Integration Tests";
            string testClass = $"Test Class: {nameof(AgentQualityEvaluatorTests)}";
            string provider = $"Model Provider: {clientMetadata?.ProviderName ?? "Unknown"}";
            string model = $"Model: {clientMetadata?.DefaultModelId ?? "Unknown"}";
            string temperature = $"Temperature: {_chatOptionsWithTools.Temperature}";
            string usesContext = $"Feature: Context";

            IEvaluator toolCallAccuracyEvaluator = new ToolCallAccuracyEvaluator();

            _agentQualityReportingConfiguration =
                DiskBasedReportingConfiguration.Create(
                    storageRootPath: Settings.Current.StorageRootPath,
                    evaluators: [],
                    chatConfiguration: chatConfigurationWithToolCalling,
                    executionName: Constants.Version,
                    tags: [version, date, projectName, testClass, provider, model, temperature]);

            _needsContextReportingConfiguration =
                DiskBasedReportingConfiguration.Create(
                    storageRootPath: Settings.Current.StorageRootPath,
                    evaluators: [toolCallAccuracyEvaluator],
                    chatConfiguration: chatConfigurationWithToolCalling,
                    executionName: Constants.Version,
                    tags: [version, date, projectName, testClass, provider, model, temperature, usesContext]);
        }
    }

    [ConditionalFact]
    public async Task ToolDefinitionsAreNeededButNotPassed()
    {
        SkipIfNotConfigured();

        await using ScenarioRun scenarioRun =
            await _needsContextReportingConfiguration.CreateScenarioRunAsync(
                scenarioName: $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(AgentQualityEvaluatorTests)}.{nameof(ToolDefinitionsAreNeededButNotPassed)}");

        (IEnumerable<ChatMessage> messages, ChatResponse response) =
            await GetConversationAsync(scenarioRun.ChatConfiguration!.ChatClient);

        EvaluationResult result = await scenarioRun.EvaluateAsync(messages, response);

        Assert.True(
            result.Metrics.Values.All(m => m.ContainsDiagnostics(d => d.Severity is EvaluationDiagnosticSeverity.Error)),
            string.Join("\r\n\r\n", result.Metrics.Values.SelectMany(m => m.Diagnostics ?? []).Select(d => d.ToString())));

        Assert.Single(result.Metrics);
        Assert.True(result.TryGet(ToolCallAccuracyEvaluator.ToolCallAccuracyMetricName, out BooleanMetric? _));
    }

    [ConditionalFact]
    public async Task ToolDefinitionsAreNeededAndPassed()
    {
        SkipIfNotConfigured();

        await using ScenarioRun scenarioRun =
            await _needsContextReportingConfiguration.CreateScenarioRunAsync(
                scenarioName: $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(AgentQualityEvaluatorTests)}.{nameof(ToolDefinitionsAreNeededAndPassed)}");

        (IEnumerable<ChatMessage> messages, ChatResponse response) =
            await GetConversationAsync(scenarioRun.ChatConfiguration!.ChatClient);

        var toolDefinitionsForToolCallAccuracyEvaluator =
            new ToolCallAccuracyEvaluatorContext(toolDefinitions: _chatOptionsWithTools.Tools!);

        EvaluationResult result =
            await scenarioRun.EvaluateAsync(
                messages,
                response,
                additionalContext: [toolDefinitionsForToolCallAccuracyEvaluator]);

        Assert.False(
            result.ContainsDiagnostics(d => d.Severity >= EvaluationDiagnosticSeverity.Warning),
            string.Join("\r\n\r\n", result.Metrics.Values.SelectMany(m => m.Diagnostics ?? []).Select(d => d.ToString())));

        Assert.Single(result.Metrics);
        Assert.True(result.TryGet(ToolCallAccuracyEvaluator.ToolCallAccuracyMetricName, out BooleanMetric? _));
    }

    [ConditionalFact]
    public async Task EvaluateToolCallsPerformedUsingSemanticKernel()
    {
        SkipIfNotConfigured();

        await using ScenarioRun scenarioRun =
            await _needsContextReportingConfiguration.CreateScenarioRunAsync(
                scenarioName: $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(AgentQualityEvaluatorTests)}.{nameof(EvaluateToolCallsPerformedUsingSemanticKernel)}");

        (IEnumerable<ChatMessage> messages, ChatResponse response, IEnumerable<AITool> toolDefinitions) =
            await GetConversationUsingSemanticKernelAsync(scenarioRun.ChatConfiguration!.ChatClient);

        var toolDefinitionsForToolCallAccuracyEvaluator = new ToolCallAccuracyEvaluatorContext(toolDefinitions);

        EvaluationResult result =
            await scenarioRun.EvaluateAsync(
                messages,
                response,
                additionalContext: [toolDefinitionsForToolCallAccuracyEvaluator]);

        Assert.False(
            result.ContainsDiagnostics(d => d.Severity >= EvaluationDiagnosticSeverity.Warning),
            string.Join("\r\n\r\n", result.Metrics.Values.SelectMany(m => m.Diagnostics ?? []).Select(d => d.ToString())));

        Assert.Single(result.Metrics);
        Assert.True(result.TryGet(ToolCallAccuracyEvaluator.ToolCallAccuracyMetricName, out BooleanMetric? _));
    }

    private static async Task<(IEnumerable<ChatMessage> messages, ChatResponse response)>
        GetConversationAsync(IChatClient chatClient)
    {
        List<ChatMessage> messages =
            [
                "You are a friendly and helpful customer service agent.".ToSystemMessage(),
                "Hi, I need help with the last 2 orders on my account #888. Could you please update me on their status?".ToUserMessage()
            ];

        ChatResponse response = await chatClient.GetResponseAsync(messages, _chatOptionsWithTools);
        return (messages, response);
    }

    private static async Task<(IEnumerable<ChatMessage> messages, ChatResponse response, IEnumerable<AITool> toolDefinitions)>
        GetConversationUsingSemanticKernelAsync(IChatClient chatClient)
    {
        IChatCompletionService chatCompletionService = chatClient.AsChatCompletionService();
        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(chatCompletionService);
        Kernel kernel = builder.Build();

        kernel.ImportPluginFromType<AgentQualityEvaluatorTests>();
        var settings =
            new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

        ChatHistory skHistory = new ChatHistory();
        skHistory.AddMessage(AuthorRole.System, "You are a friendly and helpful customer service agent.");
        skHistory.AddMessage(AuthorRole.User, "Hi, I need help with the last 2 orders on my account #888. Could you please update me on their status?");
        List<ChatMessage> messages = [.. skHistory.Select(m => m.ToChatMessage())];

        ChatMessageContent skResponse =
            await chatCompletionService.GetChatMessageContentAsync(skHistory, settings, kernel);

        skHistory.RemoveRange(0, 2); // Trim to only include the tool call and tool call result messages.
        IEnumerable<ChatMessage> toolMessages = skHistory.Select(m => m.ToChatMessage());
        ChatMessage finalResponseMessage = skResponse.ToChatMessage();
        ChatResponse response = new ChatResponse([.. toolMessages, finalResponseMessage]);

        return (messages, response, kernel.Plugins.SelectMany(p => p.AsAIFunctions()));
    }

    [KernelFunction]
    [Description("Gets the orders for a customer")]
    private static IReadOnlyList<Order> GetOrders(int accountNumber)
    {
        if (accountNumber != 888)
        {
            throw new InvalidOperationException($"Account number {accountNumber} is not valid.");
        }

        return [new Order(123), new Order(124)];
    }

    [KernelFunction]
    [Description("Gets the delivery status of an order")]
    private static OrderStatus GetOrderStatus(int orderId)
    {
        if (orderId == 123)
        {
            return new OrderStatus(orderId, "shipped", DateTime.Now.AddDays(1));
        }
        else if (orderId == 124)
        {
            return new OrderStatus(orderId, "delayed", DateTime.Now.AddDays(10));
        }
        else
        {
            throw new InvalidOperationException($"Order with ID {orderId} not found.");
        }
    }

    private record Order(int OrderId)
    {
    }

    private record OrderStatus(int OrderId, string Status, DateTime ExpectedDelivery)
    {
    }

    [MemberNotNull(nameof(_chatOptionsWithTools))]
    [MemberNotNull(nameof(_agentQualityReportingConfiguration))]
    [MemberNotNull(nameof(_needsContextReportingConfiguration))]
    private static void SkipIfNotConfigured()
    {
        if (!Settings.Current.Configured)
        {
            throw new SkipTestException("Test is not configured");
        }

        Assert.NotNull(_chatOptionsWithTools);
        Assert.NotNull(_agentQualityReportingConfiguration);
        Assert.NotNull(_needsContextReportingConfiguration);
    }
}
