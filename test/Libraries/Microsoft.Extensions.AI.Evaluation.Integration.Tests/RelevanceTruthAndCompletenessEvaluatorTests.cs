// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Integration.Tests;

public class RelevanceTruthAndCompletenessEvaluatorTests
{
    private static readonly ChatOptions _chatOptions;
    private static readonly ReportingConfiguration? _reportingConfigurationWithoutReasoning;
    private static readonly ReportingConfiguration? _reportingConfigurationWithReasoning;

    static RelevanceTruthAndCompletenessEvaluatorTests()
    {
        _chatOptions =
            new ChatOptions
            {
                Temperature = 0.0f,
                ResponseFormat = ChatResponseFormat.Text
            };

        if (Settings.Current.Configured)
        {
            IEvaluator rtcEvaluatorWithoutReasoning = new RelevanceTruthAndCompletenessEvaluator();

            _reportingConfigurationWithoutReasoning =
                DiskBasedReportingConfiguration.Create(
                    storageRootPath: Settings.Current.StorageRootPath,
                    evaluators: [rtcEvaluatorWithoutReasoning],
                    chatConfiguration: Setup.CreateChatConfiguration(),
                    executionName: Constants.Version);

            var options = new RelevanceTruthAndCompletenessEvaluatorOptions(includeReasoning: true);
            IEvaluator rtcEvaluatorWithReasoning = new RelevanceTruthAndCompletenessEvaluator(options);

            _reportingConfigurationWithReasoning =
                DiskBasedReportingConfiguration.Create(
                    storageRootPath: Settings.Current.StorageRootPath,
                    evaluators: [rtcEvaluatorWithReasoning],
                    chatConfiguration: Setup.CreateChatConfiguration(),
                    executionName: Constants.Version);
        }
    }

    [ConditionalFact]
    public async Task WithoutReasoning()
    {
        SkipIfNotConfigured();

        await using ScenarioRun scenarioRun =
            await _reportingConfigurationWithoutReasoning.CreateScenarioRunAsync(
                scenarioName: $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(RelevanceTruthAndCompletenessEvaluatorTests)}.{nameof(WithoutReasoning)}");

        IChatClient chatClient = scenarioRun.ChatConfiguration!.ChatClient;

        var messages = new List<ChatMessage>();
        string prompt = @"What is the molecular formula of ammonia?";
        ChatMessage promptMessage = prompt.ToUserMessage();
        messages.Add(promptMessage);

        ChatResponse response = await chatClient.GetResponseAsync(messages, _chatOptions);
        ChatMessage responseMessage = response.Message;
        Assert.NotNull(responseMessage.Text);

        EvaluationResult result = await scenarioRun.EvaluateAsync(promptMessage, responseMessage);

        Assert.False(result.ContainsDiagnostics(d => d.Severity >= EvaluationDiagnosticSeverity.Warning));

        NumericMetric relevance = result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.RelevanceMetricName);
        NumericMetric truth = result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.TruthMetricName);
        NumericMetric completeness = result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.CompletenessMetricName);

        Assert.True(relevance.Value >= 4, string.Format("Relevance - Reasoning: {0}", relevance.Diagnostics.Single().Message));
        Assert.True(truth.Value >= 4, string.Format("Truth - Reasoning: {0}", truth.Diagnostics.Single().Message));
        Assert.True(completeness.Value >= 4, string.Format("Completeness - Reasoning: {0}", completeness.Diagnostics.Single().Message));
    }

    [ConditionalFact]
    public async Task WithReasoning()
    {
        SkipIfNotConfigured();

        await using ScenarioRun scenarioRun =
            await _reportingConfigurationWithReasoning.CreateScenarioRunAsync(
                scenarioName: $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(RelevanceTruthAndCompletenessEvaluatorTests)}.{nameof(WithReasoning)}");

        IChatClient chatClient = scenarioRun.ChatConfiguration!.ChatClient;

        var messages = new List<ChatMessage>();
        string prompt = @"What is the molecular formula of glucose?";
        ChatMessage promptMessage = prompt.ToUserMessage();
        messages.Add(promptMessage);

        ChatResponse response = await chatClient.GetResponseAsync(messages, _chatOptions);
        ChatMessage responseMessage = response.Message;
        Assert.NotNull(responseMessage.Text);

        EvaluationResult result = await scenarioRun.EvaluateAsync(promptMessage, responseMessage);

        Assert.False(result.ContainsDiagnostics(d => d.Severity >= EvaluationDiagnosticSeverity.Warning));

        NumericMetric relevance = result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.RelevanceMetricName);
        NumericMetric truth = result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.TruthMetricName);
        NumericMetric completeness = result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.CompletenessMetricName);

        Assert.True(relevance.Value >= 4, string.Format("Relevance - Reasoning: {0}", relevance.Diagnostics.Single().Message));
        Assert.True(truth.Value >= 4, string.Format("Truth - Reasoning: {0}", truth.Diagnostics.Single().Message));
        Assert.True(completeness.Value >= 4, string.Format("Completeness - Reasoning: {0}", completeness.Diagnostics.Single().Message));
    }

    [MemberNotNull(nameof(_reportingConfigurationWithReasoning))]
    [MemberNotNull(nameof(_reportingConfigurationWithoutReasoning))]
    private static void SkipIfNotConfigured()
    {
        if (!Settings.Current.Configured)
        {
            throw new SkipTestException("Test is not configured");
        }

        Assert.NotNull(_reportingConfigurationWithReasoning);
        Assert.NotNull(_reportingConfigurationWithoutReasoning);
    }
}
