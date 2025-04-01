// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods that take it.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Integration.Tests;

public class EndToEndTests
{
    private static readonly ChatOptions _chatOptions;
    private static readonly ReportingConfiguration? _reportingConfiguration;

    static EndToEndTests()
    {
        _chatOptions =
            new ChatOptions
            {
                Temperature = 0.0f,
                ResponseFormat = ChatResponseFormat.Text
            };

        if (Settings.Current.Configured)
        {
            IEvaluator rtcEvaluator = new RelevanceTruthAndCompletenessEvaluator();
            IEvaluator coherenceEvaluator = new CoherenceEvaluator();
            IEvaluator fluencyEvaluator = new FluencyEvaluator();

            ChatConfiguration chatConfiguration = Setup.CreateChatConfiguration();
            ChatClientMetadata? clientMetadata = chatConfiguration.ChatClient.GetService<ChatClientMetadata>();

            string version = $"Product Version: {Constants.Version}";
            string date = $"Date: {DateTime.UtcNow:dddd, dd MMMM yyyy}";
            string projectName = $"Project: Integration Tests";
            string testClass = $"Test Class: {nameof(EndToEndTests)}";
            string provider = $"Model Provider: {clientMetadata?.ProviderName ?? "Unknown"}";
            string model = $"Model: {clientMetadata?.DefaultModelId ?? "Unknown"}";
            string temperature = $"Temperature: {_chatOptions.Temperature}";

            _reportingConfiguration =
                DiskBasedReportingConfiguration.Create(
                    storageRootPath: Settings.Current.StorageRootPath,
                    evaluators: [rtcEvaluator, coherenceEvaluator, fluencyEvaluator],
                    chatConfiguration: chatConfiguration,
                    executionName: Constants.Version,
                    tags: [version, date, projectName, testClass, provider, model, temperature]);
        }
    }

    [ConditionalFact]
    public async Task DistanceBetweenEarthAndMoon()
    {
        SkipIfNotConfigured();

#if NET
        await Parallel.ForAsync(1, 6, async (i, _) =>
#else
        for (int i = 1; i < 6; i++)
#endif
        {
            await using ScenarioRun scenarioRun =
                await _reportingConfiguration.CreateScenarioRunAsync(
                    scenarioName: $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(EndToEndTests)}.{nameof(DistanceBetweenEarthAndMoon)}",
                    iterationName: i.ToString());

            IChatClient chatClient = scenarioRun.ChatConfiguration!.ChatClient;

            var messages = new List<ChatMessage>();
            string prompt = "How far in miles is the moon from the earth at its closest and furthest points?";
            ChatMessage promptMessage = prompt.ToUserMessage();
            messages.Add(promptMessage);

            ChatResponse response = await chatClient.GetResponseAsync(messages, _chatOptions);

            EvaluationResult result = await scenarioRun.EvaluateAsync(promptMessage, response);

            Assert.False(
                result.ContainsDiagnostics(d => d.Severity >= EvaluationDiagnosticSeverity.Warning),
                string.Join("\r\n\r\n", result.Metrics.Values.SelectMany(m => m.Diagnostics ?? []).Select(d => d.ToString())));

            NumericMetric relevance = result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.RelevanceMetricName);
            NumericMetric truth = result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.TruthMetricName);
            NumericMetric completeness = result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.CompletenessMetricName);

            Assert.True(relevance.Value >= 4, string.Format("Relevance - Reasoning: {0}", relevance.Reason));
            Assert.True(truth.Value >= 4, string.Format("Truth - Reasoning: {0}", truth.Reason));
            Assert.True(completeness.Value >= 4, string.Format("Completeness - Reasoning: {0}", completeness.Reason));

            NumericMetric coherence = result.Get<NumericMetric>(CoherenceEvaluator.CoherenceMetricName);
            Assert.True(coherence.Value >= 4);

            NumericMetric fluency = result.Get<NumericMetric>(FluencyEvaluator.FluencyMetricName);
            Assert.True(fluency.Value >= 4);
#if NET
        });
#else
        }
#endif
    }

    [ConditionalFact]
    public async Task DistanceBetweenEarthAndVenus()
    {
        SkipIfNotConfigured();

#if NET
        await Parallel.ForAsync(1, 6, async (i, _) =>
#else
        for (int i = 1; i < 6; i++)
#endif
        {
            await using ScenarioRun scenarioRun =
                await _reportingConfiguration.CreateScenarioRunAsync(
                    scenarioName: $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(EndToEndTests)}.{nameof(DistanceBetweenEarthAndVenus)}",
                    iterationName: i.ToString());

            IChatClient chatClient = scenarioRun.ChatConfiguration!.ChatClient;

            var messages = new List<ChatMessage>();
            string prompt = @"How far in miles is the planet Venus from the Earth at its closest and furthest points?";
            ChatMessage promptMessage = prompt.ToUserMessage();
            messages.Add(promptMessage);

            ChatResponse response = await chatClient.GetResponseAsync(messages, _chatOptions);

            EvaluationResult result = await scenarioRun.EvaluateAsync(promptMessage, response);

            Assert.False(
                result.ContainsDiagnostics(d => d.Severity >= EvaluationDiagnosticSeverity.Warning),
                string.Join("\r\n\r\n", result.Metrics.Values.SelectMany(m => m.Diagnostics ?? []).Select(d => d.ToString())));

            NumericMetric relevance = result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.RelevanceMetricName);
            NumericMetric truth = result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.TruthMetricName);
            NumericMetric completeness = result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.CompletenessMetricName);

            Assert.True(relevance.Value >= 4, string.Format("Relevance - Reasoning: {0}", relevance.Reason));
            Assert.True(truth.Value >= 4, string.Format("Truth - Reasoning: {0}", truth.Reason));
            Assert.True(completeness.Value >= 4, string.Format("Completeness - Reasoning: {0}", completeness.Reason));

            NumericMetric coherence = result.Get<NumericMetric>(CoherenceEvaluator.CoherenceMetricName);
            Assert.True(coherence.Value >= 4);

            NumericMetric fluency = result.Get<NumericMetric>(FluencyEvaluator.FluencyMetricName);
            Assert.True(fluency.Value >= 4);
#if NET
        });
#else
        }
#endif
    }

    [MemberNotNull(nameof(_reportingConfiguration))]
    private static void SkipIfNotConfigured()
    {
        if (!Settings.Current.Configured)
        {
            throw new SkipTestException("Test is not configured");
        }

        Assert.NotNull(_reportingConfiguration);
    }
}
