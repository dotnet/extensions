// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.NLP;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Tests;

#pragma warning disable AIEVAL001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class F1EvaluatorTests
{
    [Fact]
    public async Task ReturnsPerfectScoreForIdenticalText()
    {
        var evaluator = new F1Evaluator();
        var context = new F1EvaluatorContext("The quick brown fox jumps over the lazy dog.");
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "The quick brown fox jumps over the lazy dog."));
        var result = await evaluator.EvaluateAsync(response, null, [context]);
        var metric = Assert.Single(result.Metrics.Values) as NumericMetric;
        Assert.NotNull(metric);
        Assert.Equal(F1Evaluator.F1MetricName, metric.Name);
        Assert.Equal(1.0, (double)metric!.Value!, 4);
        Assert.NotNull(metric.Interpretation);
        Assert.Equal(EvaluationRating.Exceptional, metric.Interpretation.Rating);
        Assert.False(metric.Interpretation.Failed);
    }

    [Fact]
    public async Task ReturnsLowScoreForCompletelyDifferentText()
    {
        var evaluator = new F1Evaluator();
        var context = new F1EvaluatorContext("The quick brown fox jumps over the lazy dog.");
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Completely unrelated sentence."));
        var result = await evaluator.EvaluateAsync(response, null, [context]);
        var metric = Assert.Single(result.Metrics.Values) as NumericMetric;
        Assert.NotNull(metric);
        Assert.Equal(F1Evaluator.F1MetricName, metric.Name);
        Assert.Equal(0.1429, (double)metric!.Value!, 4);
        Assert.NotNull(metric.Interpretation);
        Assert.Equal(EvaluationRating.Unacceptable, metric.Interpretation.Rating);
        Assert.True(metric.Interpretation.Failed);
    }

    [Fact]
    public async Task ReturnsErrorDiagnosticIfNoContext()
    {
        var evaluator = new F1Evaluator();
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Some text."));
        var result = await evaluator.EvaluateAsync(response, null, null);
        var metric = Assert.Single(result.Metrics.Values) as NumericMetric;
        Assert.NotNull(metric);
        Assert.Equal(F1Evaluator.F1MetricName, metric.Name);
        Assert.NotNull(metric.Diagnostics);
        Assert.Contains(metric.Diagnostics, d => d.Severity == EvaluationDiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("the cat is on the mat",
        "the the the the the the the", 0.30769)]
    [InlineData("It is a guide to action that ensures that the military will forever heed Party commands",
        "It is a guide to action which ensures that the military always obeys the commands of the party", 0.70589)]
    [InlineData("It is the practical guide for the army always to heed the directions of the party",
        "It is to insure the troops forever hearing the activity guidebook that party direct", 0.4000)]
    public async Task SampleCases(string reference, string hypothesis, double score)
    {
        var evaluator = new F1Evaluator();
        var context = new F1EvaluatorContext(reference);
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, hypothesis));
        var result = await evaluator.EvaluateAsync(response, null, [context]);
        var metric = Assert.Single(result.Metrics.Values) as NumericMetric;
        Assert.NotNull(metric);
        Assert.Equal(F1Evaluator.F1MetricName, metric.Name);
        Assert.Equal(score, (double)metric!.Value!, 4);
    }

    [Fact]
    public async Task ReturnsErrorDiagnosticIfEmptyResponse()
    {
        var evaluator = new F1Evaluator();
        var context = new F1EvaluatorContext("Reference text.");
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, ""));
        var result = await evaluator.EvaluateAsync(response, null, [context]);
        var metric = Assert.Single(result.Metrics.Values) as NumericMetric;
        Assert.NotNull(metric);
        Assert.Equal(F1Evaluator.F1MetricName, metric.Name);
        Assert.NotNull(metric.Diagnostics);
        Assert.Contains(metric.Diagnostics, d => d.Severity == EvaluationDiagnosticSeverity.Error);
    }

}
