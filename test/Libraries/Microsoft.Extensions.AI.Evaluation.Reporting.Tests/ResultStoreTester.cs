// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Tests;

public abstract class ResultStoreTester
{
    public abstract IResultStore CreateResultStore();

    public abstract bool IsConfigured { get; }

    private static ScenarioRunResult CreateTestResult(string scenarioName, string iterationName, string executionName)
    {
        BooleanMetric booleanMetric = new BooleanMetric("boolean", value: true);

        NumericMetric numericMetric = new NumericMetric("numeric", value: 3);
        numericMetric.AddDiagnostic(EvaluationDiagnostic.Informational("Informational Message"));

        StringMetric stringMetric = new StringMetric("string", value: "Good");

        return new ScenarioRunResult(
            scenarioName: scenarioName,
            iterationName: iterationName,
            executionName: executionName,
            creationTime: DateTime.UtcNow,
            messages: [new ChatMessage(ChatRole.User, "User prompt")],
            modelResponse: new ChatResponse(new ChatMessage(ChatRole.Assistant, "LLM response")),
            evaluationResult: new EvaluationResult(booleanMetric, numericMetric, stringMetric));
    }

    private static string ScenarioName(int n) => $"Test.Scenario.{n}";
    private static string IterationName(int n) => $"Iteration {n}";

    private static async Task<IEnumerable<(string executionName, string scenarioName, string iterationName)>> LoadResultsAsync(int n, IResultStore resultStore)
    {
        List<(string executionName, string scenarioName, string iterationName)> results = [];
        await foreach (string executionName in resultStore.GetLatestExecutionNamesAsync(n))
        {
            await foreach (string scenarioName in resultStore.GetScenarioNamesAsync(executionName))
            {
                await foreach (string iterationName in resultStore.GetIterationNamesAsync(executionName, scenarioName))
                {
                    results.Add((executionName, scenarioName, iterationName));
                }
            }
        }

        return results;
    }

    private void SkipIfNotConfigured()
    {
        if (!IsConfigured)
        {
            throw new SkipTestException("Test not configured");
        }
    }

    [ConditionalFact]
    public async Task WriteAndReadResults()
    {
        SkipIfNotConfigured();

        IResultStore resultStore = CreateResultStore();
        Assert.NotNull(resultStore);

        string newExecutionName = $"Test Execution {Path.GetRandomFileName()}";
        IEnumerable<ScenarioRunResult> testResults = [
            CreateTestResult(ScenarioName(0), IterationName(0), newExecutionName),
            CreateTestResult(ScenarioName(0), IterationName(1), newExecutionName),
            CreateTestResult(ScenarioName(0), IterationName(2), newExecutionName),
            CreateTestResult(ScenarioName(1), IterationName(0), newExecutionName),
            CreateTestResult(ScenarioName(2), IterationName(4), newExecutionName),
            CreateTestResult(ScenarioName(2), IterationName(5), newExecutionName)
        ];
        await resultStore.WriteResultsAsync(testResults);

        (string executionName, string scenarioName, string iterationName)[] results = [.. await LoadResultsAsync(1, resultStore)];
        Assert.Equal(6, results.Length);

        Assert.True(results.All(r => r.executionName == newExecutionName));

        Assert.Equal(ScenarioName(0), results[0].scenarioName);
        Assert.Equal(ScenarioName(0), results[1].scenarioName);
        Assert.Equal(ScenarioName(0), results[2].scenarioName);
        Assert.Equal(ScenarioName(1), results[3].scenarioName);
        Assert.Equal(ScenarioName(2), results[4].scenarioName);
        Assert.Equal(ScenarioName(2), results[5].scenarioName);

        Assert.Equal(IterationName(0), results[0].iterationName);
        Assert.Equal(IterationName(1), results[1].iterationName);
        Assert.Equal(IterationName(2), results[2].iterationName);
        Assert.Equal(IterationName(0), results[3].iterationName);
        Assert.Equal(IterationName(4), results[4].iterationName);
        Assert.Equal(IterationName(5), results[5].iterationName);
    }

    [ConditionalFact]
    public async Task WriteAndReadHistoricalResults()
    {
        SkipIfNotConfigured();

        IResultStore resultStore = CreateResultStore();
        Assert.NotNull(resultStore);

        string firstExecutionName = $"Test Execution {Path.GetRandomFileName()}";
        IEnumerable<ScenarioRunResult> testResults = [
            CreateTestResult(ScenarioName(0), IterationName(0), firstExecutionName),
            CreateTestResult(ScenarioName(1), IterationName(2), firstExecutionName),
            CreateTestResult(ScenarioName(2), IterationName(4), firstExecutionName),
        ];
        await resultStore.WriteResultsAsync(testResults);

        await Task.Delay(TimeSpan.FromMilliseconds(500));

        string secondExecutionName = $"Test Execution {Path.GetRandomFileName()}";
        testResults = [
            CreateTestResult(ScenarioName(0), IterationName(0), secondExecutionName),
            CreateTestResult(ScenarioName(1), IterationName(2), secondExecutionName),
            CreateTestResult(ScenarioName(2), IterationName(4), secondExecutionName),
        ];
        await resultStore.WriteResultsAsync(testResults);

        await Task.Delay(TimeSpan.FromMilliseconds(500));

        string thirdExecutionName = $"Test Execution {Path.GetRandomFileName()}";
        testResults = [
            CreateTestResult(ScenarioName(0), IterationName(0), thirdExecutionName),
            CreateTestResult(ScenarioName(1), IterationName(2), thirdExecutionName),
            CreateTestResult(ScenarioName(2), IterationName(4), thirdExecutionName),
        ];
        await resultStore.WriteResultsAsync(testResults);

        (string executionName, string scenarioName, string iterationName)[] results = [.. await LoadResultsAsync(n: 5, resultStore)];
        Assert.Equal(9, results.Length);

        Assert.True(results.Take(3).All(r => r.executionName == thirdExecutionName));
        Assert.True(results.Skip(3).Take(3).All(r => r.executionName == secondExecutionName));
        Assert.True(results.Skip(6).Take(3).All(r => r.executionName == firstExecutionName));
    }

    [ConditionalFact]
    public async Task DeleteExecutions()
    {
        SkipIfNotConfigured();

        IResultStore resultStore = CreateResultStore();
        Assert.NotNull(resultStore);

        string executionName = $"Test Execution {Path.GetRandomFileName()}";
        IEnumerable<ScenarioRunResult> testResults = [
            CreateTestResult(ScenarioName(0), IterationName(0), executionName),
            CreateTestResult(ScenarioName(0), IterationName(2), executionName),
            CreateTestResult(ScenarioName(1), IterationName(0), executionName),
            CreateTestResult(ScenarioName(2), IterationName(4), executionName),
            CreateTestResult(ScenarioName(2), IterationName(5), executionName)
        ];
        await resultStore.WriteResultsAsync(testResults);

        await resultStore.DeleteResultsAsync(executionName);

        (string executionName, string scenarioName, string iterationName)[] results = [.. await LoadResultsAsync(1, resultStore)];
        Assert.Empty(results);
    }

    [ConditionalFact]
    public async Task DeleteSomeExecutions()
    {
        SkipIfNotConfigured();

        IResultStore resultStore = CreateResultStore();
        Assert.NotNull(resultStore);

        string executionName0 = $"Test Execution {Path.GetRandomFileName()}";
        string executionName1 = $"Test Execution {Path.GetRandomFileName()}";
        IEnumerable<ScenarioRunResult> testResults = [
            CreateTestResult(ScenarioName(1), IterationName(0), executionName1),
            CreateTestResult(ScenarioName(2), IterationName(4), executionName1),
            CreateTestResult(ScenarioName(2), IterationName(5), executionName1),
            CreateTestResult(ScenarioName(0), IterationName(0), executionName0),
            CreateTestResult(ScenarioName(0), IterationName(2), executionName0),
        ];
        await resultStore.WriteResultsAsync(testResults);

        await resultStore.DeleteResultsAsync(executionName0);

        (string executionName, string scenarioName, string iterationName)[] results = [.. await LoadResultsAsync(1, resultStore)];
        Assert.Equal(3, results.Length);

        Assert.True(results.All(r => r.executionName == executionName1));

        Assert.Equal(ScenarioName(1), results[0].scenarioName);
        Assert.Equal(ScenarioName(2), results[1].scenarioName);
        Assert.Equal(ScenarioName(2), results[2].scenarioName);

        Assert.Equal(IterationName(0), results[0].iterationName);
        Assert.Equal(IterationName(4), results[1].iterationName);
        Assert.Equal(IterationName(5), results[2].iterationName);
    }

    [ConditionalFact]
    public async Task DeleteScenarios()
    {
        SkipIfNotConfigured();

        IResultStore resultStore = CreateResultStore();
        Assert.NotNull(resultStore);

        string executionName = $"Test Execution {Path.GetRandomFileName()}";
        IEnumerable<ScenarioRunResult> testResults = [
            CreateTestResult(ScenarioName(0), IterationName(0), executionName),
            CreateTestResult(ScenarioName(0), IterationName(1), executionName),
            CreateTestResult(ScenarioName(0), IterationName(2), executionName),
            CreateTestResult(ScenarioName(1), IterationName(0), executionName),
            CreateTestResult(ScenarioName(2), IterationName(4), executionName),
            CreateTestResult(ScenarioName(2), IterationName(5), executionName)
        ];
        await resultStore.WriteResultsAsync(testResults);

        await resultStore.DeleteResultsAsync(executionName, ScenarioName(0));

        (string executionName, string scenarioName, string iterationName)[] results = [.. await LoadResultsAsync(1, resultStore)];
        Assert.Equal(3, results.Length);

        Assert.True(results.All(r => r.executionName == executionName));

        Assert.Equal(ScenarioName(1), results[0].scenarioName);
        Assert.Equal(ScenarioName(2), results[1].scenarioName);
        Assert.Equal(ScenarioName(2), results[2].scenarioName);

        Assert.Equal(IterationName(0), results[0].iterationName);
        Assert.Equal(IterationName(4), results[1].iterationName);
        Assert.Equal(IterationName(5), results[2].iterationName);
    }

    [ConditionalFact]
    public async Task DeleteIterations()
    {
        SkipIfNotConfigured();

        IResultStore resultStore = CreateResultStore();
        Assert.NotNull(resultStore);

        string executionName = $"Test Execution {Path.GetRandomFileName()}";
        IEnumerable<ScenarioRunResult> testResults = [
            CreateTestResult(ScenarioName(0), IterationName(0), executionName),
            CreateTestResult(ScenarioName(0), IterationName(1), executionName),
            CreateTestResult(ScenarioName(0), IterationName(2), executionName),
            CreateTestResult(ScenarioName(1), IterationName(0), executionName),
            CreateTestResult(ScenarioName(2), IterationName(4), executionName),
            CreateTestResult(ScenarioName(2), IterationName(5), executionName)
        ];
        await resultStore.WriteResultsAsync(testResults);

        await resultStore.DeleteResultsAsync(executionName, ScenarioName(0), IterationName(2));

        (string executionName, string scenarioName, string iterationName)[] results = [.. await LoadResultsAsync(1, resultStore)];
        Assert.Equal(5, results.Length);

        Assert.True(results.All(r => r.executionName == executionName));

        Assert.Equal(ScenarioName(0), results[0].scenarioName);
        Assert.Equal(ScenarioName(0), results[1].scenarioName);
        Assert.Equal(ScenarioName(1), results[2].scenarioName);
        Assert.Equal(ScenarioName(2), results[3].scenarioName);
        Assert.Equal(ScenarioName(2), results[4].scenarioName);

        Assert.Equal(IterationName(0), results[0].iterationName);
        Assert.Equal(IterationName(1), results[1].iterationName);
        Assert.Equal(IterationName(0), results[2].iterationName);
        Assert.Equal(IterationName(4), results[3].iterationName);
        Assert.Equal(IterationName(5), results[4].iterationName);
    }
}
