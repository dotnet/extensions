// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.AI.Evaluation.Reporting.Formats;
using Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;
using Xunit;
using static Microsoft.Extensions.AI.Evaluation.Reporting.Storage.DiskBasedResponseCache;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Tests;

public class ScenarioRunResultTests
{
    [Fact]
    public void SerializeScenarioRunResult()
    {
        var booleanMetric = new BooleanMetric("boolean", value: true);
        booleanMetric.AddDiagnostics(EvaluationDiagnostic.Error("error"));
        booleanMetric.AddDiagnostics(EvaluationDiagnostic.Warning("warning"));

        var numericMetric = new NumericMetric("numeric", value: 3);
        numericMetric.AddDiagnostics(EvaluationDiagnostic.Informational("info"));

        var stringMetric = new StringMetric("string", value: "A");

        var metricWithNoValue = new EvaluationMetric("none");
        metricWithNoValue.AddDiagnostics(EvaluationDiagnostic.Error("error"));
        metricWithNoValue.AddDiagnostics(EvaluationDiagnostic.Informational("info"));

        var turn1 =
            new ChatTurnDetails(
                latency: TimeSpan.FromSeconds(1),
                model: "gpt-4o",
                usage: new UsageDetails { InputTokenCount = 10, OutputTokenCount = 20, TotalTokenCount = 30 },
                cacheKey: Guid.NewGuid().ToString(),
                cacheHit: true);

        var turn2 =
            new ChatTurnDetails(
                latency: TimeSpan.FromSeconds(2),
                model: "gpt-4o",
                usage: new UsageDetails { InputTokenCount = 20, OutputTokenCount = 30, TotalTokenCount = 50 },
                cacheKey: Guid.NewGuid().ToString(),
                cacheHit: false);

        var chatDetails = new ChatDetails(turn1, turn2);

        var entry = new ScenarioRunResult(
            scenarioName: "Test Scenario",
            iterationName: "2",
            executionName: "Test Execution",
            creationTime: DateTime.UtcNow,
            messages: [new ChatMessage(ChatRole.User, "prompt")],
            modelResponse: new ChatResponse(new ChatMessage(ChatRole.Assistant, "response")),
            evaluationResult: new EvaluationResult(booleanMetric, numericMetric, stringMetric, metricWithNoValue),
            chatDetails: chatDetails,
            tags: ["first", "second"]);

        Assert.Equal(Defaults.ReportingFormatVersion, entry.FormatVersion);

        string json = JsonSerializer.Serialize(entry, JsonUtilities.Default.ScenarioRunResultTypeInfo);
        ScenarioRunResult? deserialized = JsonSerializer.Deserialize(json, JsonUtilities.Default.ScenarioRunResultTypeInfo);

        Assert.NotNull(deserialized);
        Assert.Equal(entry.ScenarioName, deserialized!.ScenarioName);
        Assert.Equal(entry.IterationName, deserialized.IterationName);
        Assert.Equal(entry.ExecutionName, deserialized.ExecutionName);
        Assert.Equal(entry.CreationTime, deserialized.CreationTime);
        Assert.True(entry.Messages.SequenceEqual(deserialized.Messages, ChatMessageComparer.Instance));
        Assert.Equal(entry.ModelResponse, deserialized.ModelResponse, ChatResponseComparer.Instance);
        Assert.True(entry.ChatDetails!.TurnDetails.SequenceEqual(deserialized.ChatDetails!.TurnDetails!, ChatTurnDetailsComparer.Instance));
        Assert.True(entry.Tags!.SequenceEqual(deserialized.Tags!));
        Assert.Equal(entry.FormatVersion, deserialized.FormatVersion);

        ValidateEquivalence(entry.EvaluationResult, deserialized.EvaluationResult);
    }

    [Fact]
    public void SerializeDatasetCompact()
    {
        var booleanMetric = new BooleanMetric("boolean", value: true);
        booleanMetric.AddDiagnostics(EvaluationDiagnostic.Error("error"));
        booleanMetric.AddDiagnostics(EvaluationDiagnostic.Warning("warning"));

        var numericMetric = new NumericMetric("numeric", value: 3);
        numericMetric.AddDiagnostics(EvaluationDiagnostic.Informational("info"));

        var stringMetric = new StringMetric("string", value: "A");

        var metricWithNoValue = new EvaluationMetric("none");
        metricWithNoValue.AddDiagnostics(EvaluationDiagnostic.Error("error"));
        metricWithNoValue.AddDiagnostics(EvaluationDiagnostic.Informational("info"));

        var turn1 =
            new ChatTurnDetails(
                latency: TimeSpan.FromSeconds(1),
                model: "gpt-4o",
                usage: new UsageDetails { InputTokenCount = 10, OutputTokenCount = 20, TotalTokenCount = 30 },
                cacheKey: Guid.NewGuid().ToString(),
                cacheHit: true);

        var turn2 =
            new ChatTurnDetails(
                latency: TimeSpan.FromSeconds(2),
                model: "gpt-4o",
                usage: new UsageDetails { InputTokenCount = 20, OutputTokenCount = 30, TotalTokenCount = 50 },
                cacheKey: Guid.NewGuid().ToString(),
                cacheHit: false);

        var chatDetails = new ChatDetails(turn1, turn2);

        var entry = new ScenarioRunResult(
            scenarioName: "Test Scenario",
            iterationName: "2",
            executionName: "Test Execution",
            creationTime: DateTime.UtcNow,
            messages: [new ChatMessage(ChatRole.User, "prompt")],
            modelResponse: new ChatResponse(new ChatMessage(ChatRole.Assistant, "response")),
            evaluationResult: new EvaluationResult(booleanMetric, numericMetric, stringMetric, metricWithNoValue),
            chatDetails,
            tags: ["first", "second"]);

        Assert.Equal(Defaults.ReportingFormatVersion, entry.FormatVersion);

        var dataset = new Dataset([entry], createdAt: DateTime.UtcNow, generatorVersion: "1.2.3.4");

        string json = JsonSerializer.Serialize(dataset, JsonUtilities.Compact.DatasetTypeInfo);
        Dataset? deserialized = JsonSerializer.Deserialize(json, JsonUtilities.Default.DatasetTypeInfo);

        Assert.NotNull(deserialized);
        Assert.Equal(entry.ScenarioName, deserialized!.ScenarioRunResults[0].ScenarioName);
        Assert.Equal(entry.IterationName, deserialized.ScenarioRunResults[0].IterationName);
        Assert.Equal(entry.ExecutionName, deserialized.ScenarioRunResults[0].ExecutionName);
        Assert.Equal(entry.CreationTime, deserialized.ScenarioRunResults[0].CreationTime);
        Assert.True(entry.Messages.SequenceEqual(deserialized.ScenarioRunResults[0].Messages, ChatMessageComparer.Instance));
        Assert.Equal(entry.ModelResponse, deserialized.ScenarioRunResults[0].ModelResponse, ChatResponseComparer.Instance);
        Assert.True(entry.ChatDetails!.TurnDetails.SequenceEqual(deserialized.ScenarioRunResults[0].ChatDetails!.TurnDetails!, ChatTurnDetailsComparer.Instance));
        Assert.True(entry.Tags!.SequenceEqual(deserialized.ScenarioRunResults[0].Tags!));
        Assert.Equal(entry.FormatVersion, deserialized.ScenarioRunResults[0].FormatVersion);

        Assert.Single(deserialized.ScenarioRunResults);
        Assert.Equal(dataset.CreatedAt, deserialized.CreatedAt);
        Assert.Equal(dataset.GeneratorVersion, deserialized.GeneratorVersion);

        ValidateEquivalence(entry.EvaluationResult, deserialized.ScenarioRunResults[0].EvaluationResult);
    }

    [Fact]
    public void VerifyCompactSerialization()
    {
        var entry = new CacheEntry(
            scenarioName: "Scenario1",
            iterationName: "Iteration2",
            creation: DateTime.UtcNow,
            expiration: DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)));

        string defaultJson = JsonSerializer.Serialize(entry, JsonUtilities.Default.CacheEntryTypeInfo);
        string compactJson = JsonSerializer.Serialize(entry, JsonUtilities.Compact.CacheEntryTypeInfo);

        Assert.NotEqual(defaultJson, compactJson);
        Assert.True(defaultJson.Length > compactJson.Length);
        Assert.Contains("\n", defaultJson);
        Assert.DoesNotContain("\n", compactJson);
    }

    private static void ValidateEquivalence(EvaluationResult? first, EvaluationResult? second)
    {
        Assert.NotNull(first);
        Assert.NotNull(second);

        Assert.Equal(first!.Metrics.Count, second!.Metrics.Count);

        BooleanMetric booleanMetric = first.Get<BooleanMetric>("boolean");
        BooleanMetric deserializedBooleanMetric = second.Get<BooleanMetric>("boolean");
        Assert.Equal(booleanMetric.Name, deserializedBooleanMetric.Name);
        Assert.Equal(booleanMetric.Value, deserializedBooleanMetric.Value);
        Assert.Equal(booleanMetric.Diagnostics is null, deserializedBooleanMetric.Diagnostics is null);
        if (booleanMetric.Diagnostics is not null && deserializedBooleanMetric.Diagnostics is not null)
        {
            Assert.True(booleanMetric.Diagnostics.SequenceEqual(deserializedBooleanMetric.Diagnostics, DiagnosticComparer.Instance));
        }

        NumericMetric numericMetric = first.Get<NumericMetric>("numeric");
        NumericMetric deserializedNumericMetric = second.Get<NumericMetric>("numeric");
        Assert.Equal(numericMetric.Name, deserializedNumericMetric.Name);
        Assert.Equal(numericMetric.Value, deserializedNumericMetric.Value);
        Assert.Equal(numericMetric.Diagnostics is null, deserializedNumericMetric.Diagnostics is null);
        if (numericMetric.Diagnostics is not null && deserializedNumericMetric.Diagnostics is not null)
        {
            Assert.True(numericMetric.Diagnostics.SequenceEqual(deserializedNumericMetric.Diagnostics, DiagnosticComparer.Instance));
        }

        StringMetric stringMetric = first.Get<StringMetric>("string");
        StringMetric deserializedStringMetric = second.Get<StringMetric>("string");
        Assert.Equal(stringMetric.Name, deserializedStringMetric.Name);
        Assert.Equal(stringMetric.Value, deserializedStringMetric.Value);
        Assert.Equal(stringMetric.Diagnostics is null, deserializedStringMetric.Diagnostics is null);
        if (stringMetric.Diagnostics is not null && deserializedStringMetric.Diagnostics is not null)
        {
            Assert.True(stringMetric.Diagnostics.SequenceEqual(deserializedStringMetric.Diagnostics, DiagnosticComparer.Instance));
        }

        EvaluationMetric metricWithNoValue = first.Get<EvaluationMetric>("none");
        EvaluationMetric deserializedMetricWithNoValue = second.Get<EvaluationMetric>("none");
        Assert.Equal(metricWithNoValue.Name, deserializedMetricWithNoValue.Name);
        Assert.Equal(metricWithNoValue.Diagnostics is null, deserializedMetricWithNoValue.Diagnostics is null);
        if (metricWithNoValue.Diagnostics is not null && deserializedMetricWithNoValue.Diagnostics is not null)
        {
            Assert.True(metricWithNoValue.Diagnostics.SequenceEqual(deserializedMetricWithNoValue.Diagnostics, DiagnosticComparer.Instance));
        }
    }

    private class ChatMessageComparer : IEqualityComparer<ChatMessage>
    {
        public static ChatMessageComparer Instance { get; } = new ChatMessageComparer();

        public bool Equals(ChatMessage? x, ChatMessage? y)
            => x?.AuthorName == y?.AuthorName && x?.Role == y?.Role && x?.Text == y?.Text;

        public int GetHashCode(ChatMessage obj)
            => obj.Text.GetHashCode();
    }

    private class ChatResponseComparer : IEqualityComparer<ChatResponse>
    {
        public static ChatResponseComparer Instance { get; } = new ChatResponseComparer();

        public bool Equals(ChatResponse? x, ChatResponse? y)
            =>
            x is null ? y is null :
            y is not null && x.Messages.SequenceEqual(y.Messages, ChatMessageComparer.Instance);

        public int GetHashCode(ChatResponse obj)
            => obj.Text.GetHashCode();
    }

    private class DiagnosticComparer : IEqualityComparer<EvaluationDiagnostic>
    {
        public static DiagnosticComparer Instance { get; } = new DiagnosticComparer();

        public bool Equals(EvaluationDiagnostic? x, EvaluationDiagnostic? y)
            => x?.Severity == y?.Severity && x?.Message == y?.Message;

        public int GetHashCode(EvaluationDiagnostic obj)
            => obj.GetHashCode();
    }

    private class ChatTurnDetailsComparer : IEqualityComparer<ChatTurnDetails>
    {
        public static ChatTurnDetailsComparer Instance { get; } = new ChatTurnDetailsComparer();

#pragma warning disable S1067 // Expressions should not be too complex
        public bool Equals(ChatTurnDetails? x, ChatTurnDetails? y) =>
            x?.Latency == y?.Latency &&
            x?.Usage?.InputTokenCount == y?.Usage?.InputTokenCount &&
            x?.Usage?.OutputTokenCount == y?.Usage?.OutputTokenCount &&
            x?.Usage?.TotalTokenCount == y?.Usage?.TotalTokenCount &&
            x?.CacheKey == y?.CacheKey &&
            x?.CacheHit == y?.CacheHit;
#pragma warning restore S1067

        public int GetHashCode(ChatTurnDetails obj)
            => obj.GetHashCode();
    }
}
