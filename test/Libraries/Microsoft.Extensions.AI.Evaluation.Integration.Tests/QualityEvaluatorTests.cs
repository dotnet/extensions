// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods that take it.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.AI.Evaluation.Tests;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Integration.Tests;

public class QualityEvaluatorTests
{
    private static readonly ChatOptions? _chatOptions;
    private static readonly ReportingConfiguration? _qualityReportingConfiguration;
    private static readonly ReportingConfiguration? _equivalenceAndGroundednessReportingConfiguration;

    static QualityEvaluatorTests()
    {
        if (Settings.Current.Configured)
        {
            _chatOptions =
                new ChatOptions
                {
                    Temperature = 0.0f,
                    ResponseFormat = ChatResponseFormat.Text
                };

            ChatConfiguration chatConfiguration = Setup.CreateChatConfiguration();
            ChatClientMetadata? clientMetadata = chatConfiguration.ChatClient.GetService<ChatClientMetadata>();

            string version = $"Product Version: {Constants.Version}";
            string date = $"Date: {DateTime.UtcNow:dddd, dd MMMM yyyy}";
            string projectName = $"Project: Integration Tests";
            string testClass = $"Test Class: {nameof(QualityEvaluatorTests)}";
            string provider = $"Model Provider: {clientMetadata?.ProviderName ?? "Unknown"}";
            string model = $"Model: {clientMetadata?.DefaultModelId ?? "Unknown"}";
            string temperature = $"Temperature: {_chatOptions.Temperature}";
            string usesContext = $"Feature: Context";

            IEvaluator rtcEvaluator = new RelevanceTruthAndCompletenessEvaluator();
            IEvaluator coherenceEvaluator = new CoherenceEvaluator();
            IEvaluator fluencyEvaluator = new FluencyEvaluator();

            _qualityReportingConfiguration =
                DiskBasedReportingConfiguration.Create(
                    storageRootPath: Settings.Current.StorageRootPath,
                    evaluators: [rtcEvaluator, coherenceEvaluator, fluencyEvaluator],
                    chatConfiguration: chatConfiguration,
                    executionName: Constants.Version,
                    tags: [version, date, projectName, testClass, provider, model, temperature,]);

            IEvaluator groundednessEvaluator = new GroundednessEvaluator();
            IEvaluator equivalenceEvaluator = new EquivalenceEvaluator();

            _equivalenceAndGroundednessReportingConfiguration =
                DiskBasedReportingConfiguration.Create(
                    storageRootPath: Settings.Current.StorageRootPath,
                    evaluators: [groundednessEvaluator, equivalenceEvaluator],
                    chatConfiguration,
                    executionName: Constants.Version,
                    tags: [version, date, projectName, testClass, provider, model, temperature, usesContext]);
        }
    }

    [ConditionalFact]
    public async Task SampleSingleResponse()
    {
        SkipIfNotConfigured();

        await using ScenarioRun scenarioRun =
            await _qualityReportingConfiguration.CreateScenarioRunAsync(
                scenarioName: $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(QualityEvaluatorTests)}.{nameof(SampleSingleResponse)}");

        IChatClient chatClient = scenarioRun.ChatConfiguration!.ChatClient;

        var messages = new List<ChatMessage>();

        string prompt = "How far in miles is the moon from the earth at its closest and furthest points?";
        messages.Add(prompt.ToUserMessage());

        ChatResponse response = await chatClient.GetResponseAsync(messages, _chatOptions);

        EvaluationResult result = await scenarioRun.EvaluateAsync(messages, response);

        Assert.False(
            result.ContainsDiagnostics(d => d.Severity >= EvaluationDiagnosticSeverity.Warning),
            string.Join("\r\n\r\n", result.Metrics.Values.SelectMany(m => m.Diagnostics ?? []).Select(d => d.ToString())));
    }

    [ConditionalFact]
    public async Task SampleMultipleResponses()
    {
        SkipIfNotConfigured();

#if NET
        await Parallel.ForAsync(1, 6, async (i, _) =>
#else
        for (int i = 1; i < 6; i++)
#endif
        {
            await using ScenarioRun scenarioRun =
                await _qualityReportingConfiguration.CreateScenarioRunAsync(
                    scenarioName: $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(QualityEvaluatorTests)}.{nameof(SampleMultipleResponses)}",
                    iterationName: i.ToString());

            IChatClient chatClient = scenarioRun.ChatConfiguration!.ChatClient;

            var messages = new List<ChatMessage>();
            string prompt = @"How far in miles is the planet Venus from the Earth at its closest and furthest points?";
            messages.Add(prompt.ToUserMessage());

            ChatResponse response = await chatClient.GetResponseAsync(messages, _chatOptions);

            EvaluationResult result = await scenarioRun.EvaluateAsync(messages, response);

            Assert.False(
                result.ContainsDiagnostics(d => d.Severity >= EvaluationDiagnosticSeverity.Warning),
                string.Join("\r\n\r\n", result.Metrics.Values.SelectMany(m => m.Diagnostics ?? []).Select(d => d.ToString())));
#if NET
        });
#else
        }
#endif
    }

    [ConditionalFact]
    public async Task AdditionalContextIsNotPassed()
    {
        SkipIfNotConfigured();

        await using ScenarioRun scenarioRun =
            await _equivalenceAndGroundednessReportingConfiguration.CreateScenarioRunAsync(
                scenarioName: $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(QualityEvaluatorTests)}.{nameof(AdditionalContextIsNotPassed)}");

        IChatClient chatClient = scenarioRun.ChatConfiguration!.ChatClient;

        var messages = new List<ChatMessage>();
        string prompt = @"How far in miles is the planet Venus from the Earth at its closest and furthest points?";
        messages.Add(prompt.ToUserMessage());

        ChatResponse response = await chatClient.GetResponseAsync(messages, _chatOptions);

        EvaluationResult result = await scenarioRun.EvaluateAsync(messages, response);

        Assert.True(
            result.ContainsDiagnostics(d => d.Severity is EvaluationDiagnosticSeverity.Error),
            string.Join("\r\n\r\n", result.Metrics.Values.SelectMany(m => m.Diagnostics ?? []).Select(d => d.ToString())));
    }

    [ConditionalFact]
    public async Task AdditionalContextIsPassed()
    {
        SkipIfNotConfigured();

        await using ScenarioRun scenarioRun =
            await _equivalenceAndGroundednessReportingConfiguration.CreateScenarioRunAsync(
                scenarioName: $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(QualityEvaluatorTests)}.{nameof(AdditionalContextIsPassed)}");

        IChatClient chatClient = scenarioRun.ChatConfiguration!.ChatClient;

        var messages = new List<ChatMessage>();
        string prompt = @"How far in miles is the planet Venus from the Earth at its closest and furthest points?";
        messages.Add(prompt.ToUserMessage());

        ChatResponse response = await chatClient.GetResponseAsync(messages, _chatOptions);

        var baselineResponseForEquivalenceEvaluator =
            new EquivalenceEvaluatorContext(
                """
                The distance between Earth and Venus varies significantly due to the elliptical orbits of both planets
                around the Sun. At their closest approach, known as inferior conjunction, Venus can be about 24.8
                million miles away from Earth. At their furthest point, when Venus is on the opposite side of the Sun
                from Earth, known as superior conjunction, the distance can be about 162 million miles. These distances
                can vary slightly due to the specific orbital positions of the planets at any given time.
                """);

        var groundingContextForGroundednessEvaluator =
            new GroundednessEvaluatorContext(
                """
                Distance between Venus and Earth at inferior conjunction: About 24.8 million miles.
                Distance between Venus and Earth at superior conjunction: About 162 million miles.
                """);

        EvaluationResult result =
            await scenarioRun.EvaluateAsync(
                messages,
                response,
                additionalContext: [baselineResponseForEquivalenceEvaluator, groundingContextForGroundednessEvaluator]);
    }

    [MemberNotNull(nameof(_qualityReportingConfiguration))]
    [MemberNotNull(nameof(_equivalenceAndGroundednessReportingConfiguration))]
    private static void SkipIfNotConfigured()
    {
        if (!Settings.Current.Configured)
        {
            throw new SkipTestException("Test is not configured");
        }

        Assert.NotNull(_qualityReportingConfiguration);
        Assert.NotNull(_equivalenceAndGroundednessReportingConfiguration);
    }
}
