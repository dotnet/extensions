// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Integration.Tests;

public class AdditionalContextTests
{
    private static readonly ChatOptions _chatOptions;
    private static readonly ReportingConfiguration? _reportingConfiguration;

    static AdditionalContextTests()
    {
        _chatOptions =
            new ChatOptions
            {
                Temperature = 0.0f,
                ResponseFormat = ChatResponseFormat.Text
            };

        if (Settings.Current.Configured)
        {
            IEvaluator groundednessEvaluator = new GroundednessEvaluator();
            IEvaluator equivalenceEvaluator = new EquivalenceEvaluator();

            _reportingConfiguration =
                DiskBasedReportingConfiguration.Create(
                    storageRootPath: Settings.Current.StorageRootPath,
                    evaluators: [groundednessEvaluator, equivalenceEvaluator],
                    chatConfiguration: Setup.CreateChatConfiguration(),
                    executionName: Constants.Version);
        }
    }

    [ConditionalFact]
    public async Task AdditionalContextIsNotPassed()
    {
        SkipIfNotConfigured();

        await using ScenarioRun scenarioRun =
                await _reportingConfiguration.CreateScenarioRunAsync(
                    scenarioName: $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(AdditionalContextTests)}.{nameof(AdditionalContextIsNotPassed)}");

        IChatClient chatClient = scenarioRun.ChatConfiguration!.ChatClient;

        var messages = new List<ChatMessage>();
        string prompt = @"How far in miles is the planet Venus from the Earth at its closest and furthest points?";
        ChatMessage promptMessage = prompt.ToUserMessage();
        messages.Add(promptMessage);

        ChatResponse response = await chatClient.GetResponseAsync(messages, _chatOptions);
        ChatMessage responseMessage = response.Message;
        Assert.NotNull(responseMessage.Text);

        EvaluationResult result =
            await scenarioRun.EvaluateAsync(
                promptMessage,
                responseMessage);

        using var _ = new AssertionScope();

        result.ContainsDiagnostics(d => d.Severity is EvaluationDiagnosticSeverity.Error).Should().BeTrue();

        result.TryGet(EquivalenceEvaluator.EquivalenceMetricName, out NumericMetric? _).Should().BeFalse();

        NumericMetric groundedness = result.Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName);
        groundedness.Value.Should().BeGreaterThanOrEqualTo(4);
    }

    [ConditionalFact]
    public async Task AdditionalContextIsPassed()
    {
        SkipIfNotConfigured();

        await using ScenarioRun scenarioRun =
                await _reportingConfiguration.CreateScenarioRunAsync(
                    scenarioName: $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(AdditionalContextTests)}.{nameof(AdditionalContextIsPassed)}");

        IChatClient chatClient = scenarioRun.ChatConfiguration!.ChatClient;

        var messages = new List<ChatMessage>();
        string prompt = @"How far in miles is the planet Venus from the Earth at its closest and furthest points?";
        ChatMessage promptMessage = prompt.ToUserMessage();
        messages.Add(promptMessage);

        ChatResponse response = await chatClient.GetResponseAsync(messages, _chatOptions);
        ChatMessage responseMessage = response.Message;
        Assert.NotNull(responseMessage.Text);

        var baselineResponseForEquivalenceEvaluator =
            new EquivalenceEvaluatorContext(
                """
                The distance between Earth and Venus varies significantly due to the elliptical orbits of both planets
                around the Sun. At their closest approach, known as inferior conjunction, Venus can be about 23.6
                million miles away from Earth. At their furthest point, when Venus is on the opposite side of the Sun
                from Earth, known as superior conjunction, the distance can be about 162 million miles. These distances
                can vary slightly due to the specific orbital positions of the planets at any given time.
                """);

        var groundingContextForGroundednessEvaluator =
            new GroundednessEvaluatorContext(
                """
                Distance between Venus and Earth at inferior conjunction: About 23.6 million miles.
                Distance between Venus and Earth at superior conjunction: About 162 million miles.
                """);

        EvaluationResult result =
            await scenarioRun.EvaluateAsync(
                promptMessage,
                responseMessage,
                additionalContext: [baselineResponseForEquivalenceEvaluator, groundingContextForGroundednessEvaluator]);

        using var _ = new AssertionScope();

        result.ContainsDiagnostics(d => d.Severity >= EvaluationDiagnosticSeverity.Warning).Should().BeFalse();

        NumericMetric equivalence = result.Get<NumericMetric>(EquivalenceEvaluator.EquivalenceMetricName);
        equivalence.Value.Should().BeGreaterThanOrEqualTo(3);

        NumericMetric groundedness = result.Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName);
        groundedness.Value.Should().BeGreaterThanOrEqualTo(3);
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
