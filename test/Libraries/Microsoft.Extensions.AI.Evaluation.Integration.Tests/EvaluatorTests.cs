// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods that take it.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Integration.Tests;

public class EvaluatorTests
{
    private static readonly ChatOptions _chatOptions;
    private static readonly ReportingConfiguration? _reportingConfiguration;

    static EvaluatorTests()
    {
        _chatOptions =
            new ChatOptions
            {
                Temperature = 0.0f,
                ResponseFormat = ChatResponseFormat.Text
            };

        var options = new RelevanceTruthAndCompletenessEvaluatorOptions(includeReasoning: true);
        IEvaluator rtcEvaluator = new RelevanceTruthAndCompletenessEvaluator(options);
        IEvaluator coherenceEvaluator = new CoherenceEvaluator();
        IEvaluator fluencyEvaluator = new FluencyEvaluator();

        if (Settings.Current.Configured)
        {
            _reportingConfiguration =
                DiskBasedReportingConfiguration.Create(
                    storageRootPath: Settings.Current.StorageRootPath,
                    evaluators: [rtcEvaluator, coherenceEvaluator, fluencyEvaluator],
                    chatConfiguration: Setup.CreateChatConfiguration(),
                    executionName: Constants.Version);
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
                    scenarioName: $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(EvaluatorTests)}.{nameof(DistanceBetweenEarthAndMoon)}",
                    iterationName: i.ToString());

            IChatClient chatClient = scenarioRun.ChatConfiguration!.ChatClient;

            var messages = new List<ChatMessage>();
            string prompt = "How far in miles is the moon from the earth at its closest and furthest points?";
            ChatMessage promptMessage = new ChatMessage(ChatRole.User, prompt);
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
                    scenarioName: $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(EvaluatorTests)}.{nameof(DistanceBetweenEarthAndVenus)}",
                    iterationName: i.ToString());

            IChatClient chatClient = scenarioRun.ChatConfiguration!.ChatClient;

            var messages = new List<ChatMessage>();
            string prompt = @"How far in miles is the planet Venus from the Earth at its closest and furthest points?";
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
