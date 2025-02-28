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
        BooleanMetric booleanMetric = new BooleanMetric("boolean", value: true);
        booleanMetric.AddDiagnostic(EvaluationDiagnostic.Error("error"));
        booleanMetric.AddDiagnostic(EvaluationDiagnostic.Warning("warning"));

        NumericMetric numericMetric = new NumericMetric("numeric", value: 3);
        numericMetric.AddDiagnostic(EvaluationDiagnostic.Informational("info"));

        StringMetric stringMetric = new StringMetric("string", value: "A");

        EvaluationMetric metricWithNoValue = new EvaluationMetric("none");
        metricWithNoValue.AddDiagnostic(EvaluationDiagnostic.Error("error"));
        metricWithNoValue.AddDiagnostic(EvaluationDiagnostic.Informational("info"));

        var entry = new ScenarioRunResult(
            scenarioName: "Test Scenario",
            iterationName: "2",
            executionName: "Test Execution",
            creationTime: DateTime.UtcNow,
            messages: [new ChatMessage(ChatRole.User, "prompt")],
            modelResponse: new ChatMessage(ChatRole.Assistant, "response"),
            evaluationResult: new EvaluationResult(booleanMetric, numericMetric, stringMetric, metricWithNoValue));

        string json = JsonSerializer.Serialize(entry, SerializerContext.Default.ScenarioRunResult);
        ScenarioRunResult? deserialized = JsonSerializer.Deserialize<ScenarioRunResult>(json, SerializerContext.Default.ScenarioRunResult);

        Assert.NotNull(deserialized);
        Assert.Equal(entry.ScenarioName, deserialized!.ScenarioName);
        Assert.Equal(entry.IterationName, deserialized.IterationName);
        Assert.Equal(entry.ExecutionName, deserialized.ExecutionName);
        Assert.Equal(entry.CreationTime, deserialized.CreationTime);
        Assert.True(entry.Messages.SequenceEqual(deserialized.Messages, ChatMessageComparer.Instance));
        Assert.Equal(entry.ModelResponse, deserialized.ModelResponse, ChatMessageComparer.Instance);

        ValidateEquivalence(entry.EvaluationResult, deserialized.EvaluationResult);
    }

    [Fact]
    public void SerializeDatasetCompact()
    {
        BooleanMetric booleanMetric = new BooleanMetric("boolean", value: true);
        booleanMetric.AddDiagnostic(EvaluationDiagnostic.Error("error"));
        booleanMetric.AddDiagnostic(EvaluationDiagnostic.Warning("warning"));

        NumericMetric numericMetric = new NumericMetric("numeric", value: 3);
        numericMetric.AddDiagnostic(EvaluationDiagnostic.Informational("info"));

        StringMetric stringMetric = new StringMetric("string", value: "A");

        EvaluationMetric metricWithNoValue = new EvaluationMetric("none");
        metricWithNoValue.AddDiagnostic(EvaluationDiagnostic.Error("error"));
        metricWithNoValue.AddDiagnostic(EvaluationDiagnostic.Informational("info"));

        var entry = new ScenarioRunResult(
            scenarioName: "Test Scenario",
            iterationName: "2",
            executionName: "Test Execution",
            creationTime: DateTime.UtcNow,
            messages: [new ChatMessage(ChatRole.User, "prompt")],
            modelResponse: new ChatMessage(ChatRole.Assistant, "response"),
            evaluationResult: new EvaluationResult(booleanMetric, numericMetric, stringMetric, metricWithNoValue));

        var dataset = new Dataset([entry], createdAt: DateTime.UtcNow, generatorVersion: "1.2.3.4");

        string json = JsonSerializer.Serialize(dataset, SerializerContext.Compact.Dataset);
        Dataset? deserialized = JsonSerializer.Deserialize(json, SerializerContext.Default.Dataset);

        Assert.NotNull(deserialized);
        Assert.Equal(entry.ScenarioName, deserialized!.ScenarioRunResults[0].ScenarioName);
        Assert.Equal(entry.IterationName, deserialized.ScenarioRunResults[0].IterationName);
        Assert.Equal(entry.ExecutionName, deserialized.ScenarioRunResults[0].ExecutionName);
        Assert.Equal(entry.CreationTime, deserialized.ScenarioRunResults[0].CreationTime);
        Assert.True(entry.Messages.SequenceEqual(deserialized.ScenarioRunResults[0].Messages, ChatMessageComparer.Instance));
        Assert.Equal(entry.ModelResponse, deserialized.ScenarioRunResults[0].ModelResponse, ChatMessageComparer.Instance);

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

        string defaultJson = JsonSerializer.Serialize(entry, SerializerContext.Default.CacheEntry);
        string compactJson = JsonSerializer.Serialize(entry, SerializerContext.Compact.CacheEntry);

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
        Assert.True(booleanMetric.Diagnostics.SequenceEqual(deserializedBooleanMetric.Diagnostics, DiagnosticComparer.Instance));

        NumericMetric numericMetric = first.Get<NumericMetric>("numeric");
        NumericMetric deserializedNumericMetric = second.Get<NumericMetric>("numeric");
        Assert.Equal(numericMetric.Name, deserializedNumericMetric.Name);
        Assert.Equal(numericMetric.Value, deserializedNumericMetric.Value);
        Assert.True(numericMetric.Diagnostics.SequenceEqual(deserializedNumericMetric.Diagnostics, DiagnosticComparer.Instance));

        StringMetric stringMetric = first.Get<StringMetric>("string");
        StringMetric deserializedStringMetric = second.Get<StringMetric>("string");
        Assert.Equal(stringMetric.Name, deserializedStringMetric.Name);
        Assert.Equal(stringMetric.Value, deserializedStringMetric.Value);
        Assert.True(stringMetric.Diagnostics.SequenceEqual(deserializedStringMetric.Diagnostics, DiagnosticComparer.Instance));

        EvaluationMetric metricWithNoValue = first.Get<EvaluationMetric>("none");
        EvaluationMetric deserializedMetricWithNoValue = second.Get<EvaluationMetric>("none");
        Assert.Equal(metricWithNoValue.Name, deserializedMetricWithNoValue.Name);
        Assert.True(metricWithNoValue.Diagnostics.SequenceEqual(deserializedMetricWithNoValue.Diagnostics, DiagnosticComparer.Instance));
    }

    private class ChatMessageComparer : IEqualityComparer<ChatMessage>
    {
        public static ChatMessageComparer Instance { get; } = new ChatMessageComparer();

        public bool Equals(ChatMessage? x, ChatMessage? y)
            => x?.AuthorName == y?.AuthorName && x?.Role == y?.Role && x?.Text == y?.Text;

        public int GetHashCode(ChatMessage obj)
            => obj.GetHashCode();
    }

    private class DiagnosticComparer : IEqualityComparer<EvaluationDiagnostic>
    {
        public static DiagnosticComparer Instance { get; } = new DiagnosticComparer();

        public bool Equals(EvaluationDiagnostic? x, EvaluationDiagnostic? y)
            => x?.Severity == y?.Severity && x?.Message == y?.Message;

        public int GetHashCode(EvaluationDiagnostic obj)
            => obj.GetHashCode();
    }
}
