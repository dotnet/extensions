// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Integration.Tests;

public class ResultsTests
{
    private static readonly ChatMessage _testResponse = "Test response".ToAssistantMessage();

    public static ReportingConfiguration CreateReportingConfiguration(IEvaluator evaluator) =>
        DiskBasedReportingConfiguration.Create(
            storageRootPath: Settings.Current.Configured ?
                Settings.Current.StorageRootPath :
                Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()),
            evaluators: [evaluator],
            chatConfiguration: null, // Not needed for this test
            executionName: Constants.Version);

    #region Interpretations
    private static EvaluationMetricInterpretation? FailIfValueIsTrue(EvaluationMetric m)
    {
        const bool PassingValue = false;

        switch (m)
        {
            case BooleanMetric booleanMetric:
                if (booleanMetric.Value is bool value)
                {
                    return value is PassingValue
                        ? new EvaluationMetricInterpretation(rating: EvaluationRating.Exceptional)
                        : new EvaluationMetricInterpretation(
                            rating: EvaluationRating.Unacceptable,
                            failed: true,
                            reason: $"Value is {value}.");
                }

                break;

            default:
                throw new InvalidOperationException();
        }

        return null;
    }

    private static EvaluationMetricInterpretation? FailIfValueIsLessThan4(EvaluationMetric m)
    {
        const double MinimumPassingScore = 4.0;

        switch (m)
        {
            case NumericMetric numericMetric:
                if (numericMetric.Value is double value)
                {
                    EvaluationRating rating = value switch
                    {
                        > 5.0 => EvaluationRating.Inconclusive,
                        > 4.0 and <= 5.0 => EvaluationRating.Exceptional,
                        > 3.0 and <= 4.0 => EvaluationRating.Good,
                        > 2.0 and <= 3.0 => EvaluationRating.Average,
                        > 1.0 and <= 2.0 => EvaluationRating.Poor,
                        > 0.0 and <= 1.0 => EvaluationRating.Unacceptable,
                        <= 0.0 => EvaluationRating.Inconclusive,
                        _ => EvaluationRating.Inconclusive,
                    };

                    return
                        value < MinimumPassingScore &&
                        rating is not (EvaluationRating.Inconclusive or EvaluationRating.Unknown)
                            ? new EvaluationMetricInterpretation(
                                rating,
                                failed: true,
                                reason: $"Value is less than {MinimumPassingScore}.")
                            : new EvaluationMetricInterpretation(rating);
                }

                break;

            default:
                throw new InvalidOperationException();
        }

        return null;
    }

#pragma warning disable S1067 // Expressions should not be too complex.
    private static EvaluationMetricInterpretation? FailIfValueIsMissing(EvaluationMetric m) =>
        (m is NumericMetric s && s.Value is not null) ||
        (m is BooleanMetric b && b.Value is not null) ||
        (m is StringMetric e && e.Value is not null)
            ? new EvaluationMetricInterpretation(EvaluationRating.Good)
            : new EvaluationMetricInterpretation(EvaluationRating.Unacceptable, failed: true, "Value is missing");
#pragma warning restore S1067

    private enum MeasurementSystem
    {
        None,
        Unknown,
        Metric,
        Imperial,
        USCustomary,
        Nautical,
        Astronomical,
        Multiple
    }

    private static EvaluationMetricInterpretation? FailIfNotImperialOrUSCustomary(EvaluationMetric m)
    {
        if (m is not StringMetric e)
        {
            return null;
        }

        if (e.Value is null)
        {
            return new EvaluationMetricInterpretation(EvaluationRating.Unknown, failed: true, "Value is missing");
        }

        if (!Enum.TryParse(e.Value, out MeasurementSystem measurementSystem))
        {
            return new EvaluationMetricInterpretation(EvaluationRating.Inconclusive, failed: true, $"Value {e.Value} is not an allowed value");
        }

        if (measurementSystem is MeasurementSystem.USCustomary or MeasurementSystem.Imperial)
        {
            return new EvaluationMetricInterpretation(EvaluationRating.Exceptional, reason: $"Value is {e.Value}");
        }

        return new EvaluationMetricInterpretation(EvaluationRating.Unacceptable, failed: true, reason: $"Value is {e.Value}");
    }
    #endregion

    [Fact]
    public async Task ResultWithBooleanMetric()
    {
        var evaluator = new TestEvaluator();
        ReportingConfiguration reportingConfiguration = CreateReportingConfiguration(evaluator);

        var metricA = new BooleanMetric("Metric with value false", false);
        var metricB = new BooleanMetric("Metric with value true", true);
        var metricC = new BooleanMetric("Metric without value");
        evaluator.TestMetrics = [metricA, metricB, metricC];

        await using ScenarioRun scenarioRun =
            await reportingConfiguration.CreateScenarioRunAsync(
                $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(ResultsTests)}.{nameof(ResultWithBooleanMetric)}");

        EvaluationResult result = await scenarioRun.EvaluateAsync(_testResponse);

        Assert.True(result.Metrics.Values.All(m => m.Interpretation is null));
        Assert.Null(metricA.Interpretation);
        Assert.Null(metricB.Interpretation);
        Assert.Null(metricC.Interpretation);

        Assert.False(result.ContainsDiagnostics());
    }

    [Fact]
    public async Task ResultWithBooleanMetricAndInterpretation()
    {
        var evaluator = new TestEvaluator();
        ReportingConfiguration reportingConfiguration = CreateReportingConfiguration(evaluator);

        var metricA = new BooleanMetric("Metric with value false", false);
        var metricB = new BooleanMetric("Metric with value true", true);
        var metricC = new BooleanMetric("Metric without value");
        evaluator.TestMetrics = [metricA, metricB, metricC];

        await using ScenarioRun scenarioRun =
            await reportingConfiguration.CreateScenarioRunAsync(
                $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(ResultsTests)}.{nameof(ResultWithBooleanMetricAndInterpretation)}");

        EvaluationResult result = await scenarioRun.EvaluateAsync(_testResponse);
        result.Interpret(FailIfValueIsTrue);

        Assert.NotNull(metricA.Interpretation);
        Assert.False(metricA.Interpretation!.Failed);
        Assert.NotNull(metricB.Interpretation);
        Assert.True(metricB.Interpretation!.Failed);
        Assert.Null(metricC.Interpretation);

        Assert.False(result.ContainsDiagnostics());
    }

    [Fact]
    public async Task ResultWithStringMetric()
    {
        var evaluator = new TestEvaluator();
        ReportingConfiguration reportingConfiguration = CreateReportingConfiguration(evaluator);

        var allowedValues =
            new HashSet<string>
            {
                "None",
                "Unknown",
                "Metric",
                "Imperial",
                "USCustomary",
                "Nautical",
                "Astronomical",
                "Multiple"
            };

        var metricA = new StringMetric("Measurement System: None", "None");
        var metricB = new StringMetric("Measurement System: Unknown", "Unknown");
        var metricC = new StringMetric("Measurement System: Metric", "Metric");
        var metricD = new StringMetric("Measurement System: Imperial", "Imperial");
        var metricE = new StringMetric("Measurement System: USCustomary", "UsCustomary");
        var metricF = new StringMetric("Measurement System: Nautical", "Nautical");
        var metricG = new StringMetric("Measurement System: Astronomical", "Astronomical");
        var metricH = new StringMetric("Measurement System: Multiple", "Multiple");
        var metricI = new StringMetric("Measurement System: Blah", "Blah");
        var metricJ = new StringMetric("Measurement System: Empty", "");
        var metricK = new StringMetric("Measurement System: Null");

        evaluator.TestMetrics =
            [metricA, metricB, metricC, metricD, metricE, metricF, metricG, metricH, metricI, metricJ, metricK];

        await using ScenarioRun scenarioRun =
            await reportingConfiguration.CreateScenarioRunAsync(
                $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(ResultsTests)}.{nameof(ResultWithStringMetric)}");

        EvaluationResult result = await scenarioRun.EvaluateAsync(_testResponse);

        Assert.Null(metricA.Interpretation);
        Assert.Null(metricB.Interpretation);
        Assert.Null(metricC.Interpretation);
        Assert.Null(metricD.Interpretation);
        Assert.Null(metricE.Interpretation);
        Assert.Null(metricF.Interpretation);
        Assert.Null(metricG.Interpretation);
        Assert.Null(metricH.Interpretation);
        Assert.Null(metricI.Interpretation);
        Assert.Null(metricJ.Interpretation);
        Assert.Null(metricK.Interpretation);

        Assert.False(result.ContainsDiagnostics());
    }

    [Fact]
    public async Task ResultWithStringMetricAndInterpretation()
    {
        var evaluator = new TestEvaluator();
        ReportingConfiguration reportingConfiguration = CreateReportingConfiguration(evaluator);

        var allowedValues =
            new HashSet<string>
            {
                "None",
                "Unknown",
                "Metric",
                "Imperial",
                "USCustomary",
                "Nautical",
                "Astronomical",
                "Multiple"
            };

        var metricA = new StringMetric("Measurement System: None", "None");
        var metricB = new StringMetric("Measurement System: Unknown", "Unknown");
        var metricC = new StringMetric("Measurement System: Metric", "Metric");
        var metricD = new StringMetric("Measurement System: Imperial", "Imperial");
        var metricE = new StringMetric("Measurement System: USCustomary", "USCustomary");
        var metricF = new StringMetric("Measurement System: Nautical", "Nautical");
        var metricG = new StringMetric("Measurement System: Astronomical", "Astronomical");
        var metricH = new StringMetric("Measurement System: Multiple", "Multiple");
        var metricI = new StringMetric("Measurement System: Blah", "Blah");
        var metricJ = new StringMetric("Measurement System: Empty", "");
        var metricK = new StringMetric("Measurement System: Null");

        evaluator.TestMetrics =
            [metricA, metricB, metricC, metricD, metricE, metricF, metricG, metricH, metricI, metricJ, metricK];

        await using ScenarioRun scenarioRun =
            await reportingConfiguration.CreateScenarioRunAsync(
                $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(ResultsTests)}.{nameof(ResultWithStringMetricAndInterpretation)}");

        EvaluationResult result = await scenarioRun.EvaluateAsync(_testResponse);
        result.Interpret(FailIfNotImperialOrUSCustomary);

        Assert.NotNull(metricA.Interpretation);
        Assert.True(metricA.Interpretation!.Failed);
        Assert.NotNull(metricB.Interpretation);
        Assert.True(metricB.Interpretation!.Failed);
        Assert.NotNull(metricC.Interpretation);
        Assert.True(metricC.Interpretation!.Failed);
        Assert.NotNull(metricD.Interpretation);
        Assert.False(metricD.Interpretation!.Failed);
        Assert.NotNull(metricE.Interpretation);
        Assert.False(metricE.Interpretation!.Failed);
        Assert.NotNull(metricF.Interpretation);
        Assert.True(metricF.Interpretation!.Failed);
        Assert.NotNull(metricG.Interpretation);
        Assert.True(metricG.Interpretation!.Failed);
        Assert.NotNull(metricH.Interpretation);
        Assert.True(metricH.Interpretation!.Failed);
        Assert.NotNull(metricI.Interpretation);
        Assert.True(metricI.Interpretation!.Failed);
        Assert.NotNull(metricJ.Interpretation);
        Assert.True(metricJ.Interpretation!.Failed);
        Assert.NotNull(metricK.Interpretation);
        Assert.True(metricK.Interpretation!.Failed);

        Assert.False(result.ContainsDiagnostics());
    }

    [Fact]
    public async Task ResultWithNumericMetrics()
    {
        var evaluator = new TestEvaluator();
        ReportingConfiguration reportingConfiguration = CreateReportingConfiguration(evaluator);

        var metricA = new NumericMetric("Metric with value 0", 0);
        var metricB = new NumericMetric("Metric with value 1", 1);
        var metricC = new NumericMetric("Metric with value 2", 2);
        var metricD = new NumericMetric("Metric with value 3", 3);
        var metricE = new NumericMetric("Metric with value 4", 4);
        var metricF = new NumericMetric("Metric with value 5", 5);
        var metricG = new NumericMetric("Metric with value 6", 6);
        var metricH = new NumericMetric("Metric with no value");
        evaluator.TestMetrics = [metricA, metricB, metricC, metricD, metricE, metricF, metricG, metricH];

        await using ScenarioRun scenarioRun =
            await reportingConfiguration.CreateScenarioRunAsync(
                $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(ResultsTests)}.{nameof(ResultWithNumericMetrics)}");

        EvaluationResult result = await scenarioRun.EvaluateAsync(_testResponse);

        Assert.True(result.Metrics.Values.All(m => m.Interpretation is null));
        Assert.Null(metricA.Interpretation);
        Assert.Null(metricB.Interpretation);
        Assert.Null(metricC.Interpretation);
        Assert.Null(metricD.Interpretation);
        Assert.Null(metricE.Interpretation);
        Assert.Null(metricF.Interpretation);
        Assert.Null(metricG.Interpretation);
        Assert.Null(metricH.Interpretation);

        Assert.False(result.ContainsDiagnostics());
    }

    [Fact]
    public async Task ResultWithNumericMetricsAndInterpretation()
    {
        var evaluator = new TestEvaluator();
        ReportingConfiguration reportingConfiguration = CreateReportingConfiguration(evaluator);

        var metricA = new NumericMetric("Metric with value 0", 0);
        var metricB = new NumericMetric("Metric with value 1", 1);
        var metricC = new NumericMetric("Metric with value 2", 2);
        var metricD = new NumericMetric("Metric with value 3", 3);
        var metricE = new NumericMetric("Metric with value 4", 4);
        var metricF = new NumericMetric("Metric with value 5", 5);
        var metricG = new NumericMetric("Metric with value 6", 6);
        var metricH = new NumericMetric("Metric with no value");
        evaluator.TestMetrics = [metricA, metricB, metricC, metricD, metricE, metricF, metricG, metricH];

        await using ScenarioRun scenarioRun =
            await reportingConfiguration.CreateScenarioRunAsync(
                $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(ResultsTests)}.{nameof(ResultWithNumericMetricsAndInterpretation)}");

        EvaluationResult result = await scenarioRun.EvaluateAsync(_testResponse);
        result.Interpret(FailIfValueIsLessThan4);

        Assert.NotNull(metricA.Interpretation);
        Assert.False(metricA.Interpretation!.Failed);
        Assert.NotNull(metricB.Interpretation);
        Assert.True(metricB.Interpretation!.Failed);
        Assert.NotNull(metricC.Interpretation);
        Assert.True(metricC.Interpretation!.Failed);
        Assert.NotNull(metricD.Interpretation);
        Assert.True(metricD.Interpretation!.Failed);
        Assert.NotNull(metricE.Interpretation);
        Assert.False(metricE.Interpretation!.Failed);
        Assert.NotNull(metricF.Interpretation);
        Assert.False(metricF.Interpretation!.Failed);
        Assert.NotNull(metricG.Interpretation);
        Assert.False(metricG.Interpretation!.Failed);
        Assert.Null(metricH.Interpretation);

        Assert.False(result.ContainsDiagnostics());
    }

    [Fact]
    public async Task ResultWithDiagnosticsOnUninterpretedMetrics()
    {
        var evaluator = new TestEvaluator();
        ReportingConfiguration reportingConfiguration = CreateReportingConfiguration(evaluator);

        var metric1 = new BooleanMetric("Metric with all diagnostic severities");
        metric1.AddDiagnostic(EvaluationDiagnostic.Error("Error 1"));
        metric1.AddDiagnostic(EvaluationDiagnostic.Error("Error 2"));
        metric1.AddDiagnostic(EvaluationDiagnostic.Warning("Warning 1"));
        metric1.AddDiagnostic(EvaluationDiagnostic.Informational("Informational 1"));
        metric1.AddDiagnostic(EvaluationDiagnostic.Informational("Informational 2"));

        var metric2 = new BooleanMetric("Metric with warning and informational diagnostics");
        metric2.AddDiagnostic(EvaluationDiagnostic.Warning("Warning 1"));
        metric2.AddDiagnostic(EvaluationDiagnostic.Warning("Warning 2"));
        metric2.AddDiagnostic(EvaluationDiagnostic.Informational("Informational 2"));

        var metric3 = new EvaluationMetric("Metric with error diagnostics only");
        metric3.AddDiagnostic(EvaluationDiagnostic.Error("Error 1"));
        metric3.AddDiagnostic(EvaluationDiagnostic.Error("Error 2"));

        HashSet<string> allowedValues = ["A", "B", "C"];
        var metric4 = new StringMetric("Metric with warning diagnostics only");
        metric4.AddDiagnostic(EvaluationDiagnostic.Warning("Warning 1"));
        metric4.AddDiagnostic(EvaluationDiagnostic.Warning("Warning 2"));

        var metric5 = new NumericMetric("Metric with informational diagnostics only");
        metric5.AddDiagnostic(EvaluationDiagnostic.Informational("Informational 1"));

        evaluator.TestMetrics = [metric1, metric2, metric3, metric4, metric5];

        await using ScenarioRun scenarioRun =
            await reportingConfiguration.CreateScenarioRunAsync(
                $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(ResultsTests)}.{nameof(ResultWithDiagnosticsOnUninterpretedMetrics)}");

        EvaluationResult result = await scenarioRun.EvaluateAsync(_testResponse);

        Assert.Null(metric1.Interpretation);
        Assert.Null(metric2.Interpretation);
        Assert.Null(metric3.Interpretation);
        Assert.Null(metric4.Interpretation);
        Assert.Null(metric5.Interpretation);

        Assert.True(result.ContainsDiagnostics());
    }

    [Fact]
    public async Task ResultWithDiagnosticsOnFailingMetrics()
    {
        var evaluator = new TestEvaluator();
        ReportingConfiguration reportingConfiguration = CreateReportingConfiguration(evaluator);

        var metric1 = new BooleanMetric("Metric with all diagnostic severities");
        metric1.AddDiagnostic(EvaluationDiagnostic.Error("Error 1"));
        metric1.AddDiagnostic(EvaluationDiagnostic.Error("Error 2"));
        metric1.AddDiagnostic(EvaluationDiagnostic.Warning("Warning 1"));
        metric1.AddDiagnostic(EvaluationDiagnostic.Informational("Informational 1"));
        metric1.AddDiagnostic(EvaluationDiagnostic.Informational("Informational 2"));

        var metric2 = new BooleanMetric("Metric with warning and informational diagnostics");
        metric2.AddDiagnostic(EvaluationDiagnostic.Warning("Warning 1"));
        metric2.AddDiagnostic(EvaluationDiagnostic.Warning("Warning 2"));
        metric2.AddDiagnostic(EvaluationDiagnostic.Informational("Informational 2"));

        var metric3 = new EvaluationMetric("Metric with error diagnostics only");
        metric3.AddDiagnostic(EvaluationDiagnostic.Error("Error 1"));
        metric3.AddDiagnostic(EvaluationDiagnostic.Error("Error 2"));

        HashSet<string> allowedValues = ["A", "B", "C"];
        var metric4 = new StringMetric("Metric with warning diagnostics only");
        metric4.AddDiagnostic(EvaluationDiagnostic.Warning("Warning 1"));
        metric4.AddDiagnostic(EvaluationDiagnostic.Warning("Warning 2"));

        var metric5 = new NumericMetric("Metric with informational diagnostics only");
        metric5.AddDiagnostic(EvaluationDiagnostic.Informational("Informational 1"));

        evaluator.TestMetrics = [metric1, metric2, metric3, metric4, metric5];

        await using ScenarioRun scenarioRun =
            await reportingConfiguration.CreateScenarioRunAsync(
                $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(ResultsTests)}.{nameof(ResultWithDiagnosticsOnFailingMetrics)}");

        EvaluationResult result = await scenarioRun.EvaluateAsync(_testResponse);
        result.Interpret(FailIfValueIsMissing);

        Assert.NotNull(metric1.Interpretation);
        Assert.True(metric1.Interpretation!.Failed);
        Assert.NotNull(metric2.Interpretation);
        Assert.True(metric2.Interpretation!.Failed);
        Assert.NotNull(metric3.Interpretation);
        Assert.True(metric3.Interpretation!.Failed);
        Assert.NotNull(metric4.Interpretation);
        Assert.True(metric4.Interpretation!.Failed);
        Assert.NotNull(metric5.Interpretation);
        Assert.True(metric5.Interpretation!.Failed);

        Assert.True(result.ContainsDiagnostics());
    }

    [Fact]
    public async Task ResultWithDiagnosticsOnPassingMetrics()
    {
        var evaluator = new TestEvaluator();
        ReportingConfiguration reportingConfiguration = CreateReportingConfiguration(evaluator);

        var metric1 = new BooleanMetric("Metric with all diagnostic severities", value: true);
        metric1.AddDiagnostic(EvaluationDiagnostic.Error("Error 1"));
        metric1.AddDiagnostic(EvaluationDiagnostic.Error("Error 2"));
        metric1.AddDiagnostic(EvaluationDiagnostic.Warning("Warning 1"));
        metric1.AddDiagnostic(EvaluationDiagnostic.Informational("Informational 1"));
        metric1.AddDiagnostic(EvaluationDiagnostic.Informational("Informational 2"));

        var metric2 = new BooleanMetric("Metric with warning and informational diagnostics", value: true);
        metric2.AddDiagnostic(EvaluationDiagnostic.Warning("Warning 1"));
        metric2.AddDiagnostic(EvaluationDiagnostic.Warning("Warning 2"));
        metric2.AddDiagnostic(EvaluationDiagnostic.Informational("Informational 2"));

        var metric3 = new NumericMetric("Metric with error diagnostics only", value: 5);
        metric3.AddDiagnostic(EvaluationDiagnostic.Error("Error 1"));
        metric3.AddDiagnostic(EvaluationDiagnostic.Error("Error 2"));

        HashSet<string> allowedValues = ["A", "B", "C"];
        var metric4 = new StringMetric("Metric with warning diagnostics only", value: "A");
        metric4.AddDiagnostic(EvaluationDiagnostic.Warning("Warning 1"));
        metric4.AddDiagnostic(EvaluationDiagnostic.Warning("Warning 2"));

        var metric5 = new NumericMetric("Metric with informational diagnostics only", value: 4);
        metric5.AddDiagnostic(EvaluationDiagnostic.Informational("Informational 1"));

        evaluator.TestMetrics = [metric1, metric2, metric3, metric4, metric5];

        await using ScenarioRun scenarioRun =
            await reportingConfiguration.CreateScenarioRunAsync(
                $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(ResultsTests)}.{nameof(ResultWithDiagnosticsOnPassingMetrics)}");

        EvaluationResult result = await scenarioRun.EvaluateAsync(_testResponse);
        result.Interpret(FailIfValueIsMissing);

        Assert.NotNull(metric1.Interpretation);
        Assert.False(metric1.Interpretation!.Failed);
        Assert.NotNull(metric2.Interpretation);
        Assert.False(metric2.Interpretation!.Failed);
        Assert.NotNull(metric3.Interpretation);
        Assert.False(metric3.Interpretation!.Failed);
        Assert.NotNull(metric4.Interpretation);
        Assert.False(metric4.Interpretation!.Failed);
        Assert.NotNull(metric5.Interpretation);
        Assert.False(metric5.Interpretation!.Failed);

        Assert.True(result.ContainsDiagnostics());
    }

    [Fact]
    public async Task ResultWithException()
    {
        var evaluator = new TestEvaluator();
        ReportingConfiguration reportingConfiguration = CreateReportingConfiguration(evaluator);

        var metric = new BooleanMetric("Condition", true);

        evaluator.TestMetrics = [metric];
        evaluator.ThrowOnEvaluate = true;

        await using ScenarioRun scenarioRun =
            await reportingConfiguration.CreateScenarioRunAsync(
                $"Microsoft.Extensions.AI.Evaluation.Integration.Tests.{nameof(ResultsTests)}.{nameof(ResultWithException)}");

        EvaluationResult result = await scenarioRun.EvaluateAsync(_testResponse);

        Assert.True(result.ContainsDiagnostics(d => d.Severity == EvaluationDiagnosticSeverity.Error));
    }
}
