// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Sampling;

public class LogSamplingRuleSelectorTests
{
    [Fact]
    public void SelectsRightRule()
    {
        // Arrange
        var rules = new List<ILogSamplingFilterRule>
        {
            new ProbabilisticSamplerFilterRule (probability: 0),
            new ProbabilisticSamplerFilterRule (probability: 0, eventId: 1),
            new ProbabilisticSamplerFilterRule (probability: 0, logLevel: LogLevel.Information, eventId: 1 ),
            new ProbabilisticSamplerFilterRule (probability: 0, logLevel: LogLevel.Information, eventId: 1 ),
            new ProbabilisticSamplerFilterRule (probability: 0, logLevel: LogLevel.Warning),
            new ProbabilisticSamplerFilterRule (probability: 0, logLevel : LogLevel.Warning, eventId : 2),
            new ProbabilisticSamplerFilterRule (probability: 0, logLevel : LogLevel.Warning, eventId : 1),
            new ProbabilisticSamplerFilterRule (probability: 0, categoryName: "Program1.MyLogger", logLevel: LogLevel.Warning, eventId: 1),
            new ProbabilisticSamplerFilterRule (probability : 0, categoryName : "Program.*MyLogger1", logLevel : LogLevel.Warning, eventId : 1),
            new ProbabilisticSamplerFilterRule (probability : 0, categoryName : "Program.MyLogger", logLevel : LogLevel.Warning, eventId : 1), // the best rule
            new ProbabilisticSamplerFilterRule (probability : 0, categoryName : "Program.MyLogger", logLevel : LogLevel.Warning, eventId : 2),
            new ProbabilisticSamplerFilterRule (probability : 0, categoryName : "Program.MyLogger", eventId : 1),
            new ProbabilisticSamplerFilterRule (probability : 0, logLevel : LogLevel.Warning, eventId : 1),
            new ProbabilisticSamplerFilterRule (probability : 0, categoryName : "Program", logLevel : LogLevel.Warning, eventId : 1),
            new ProbabilisticSamplerFilterRule (probability : 0, categoryName : "Program.MyLogger", logLevel : LogLevel.Warning),
            new ProbabilisticSamplerFilterRule (probability : 0, categoryName : "Program.MyLogger", logLevel : LogLevel.Error, eventId : 1),
        };

        // Act
        LogSamplingRuleSelector.Select(rules, "Program.MyLogger", LogLevel.Warning, 1, out var actualResult);

        // Assert
        Assert.Same(rules[9], actualResult);
    }

    [Fact]
    public void WhenManyRuleApply_SelectsLast()
    {
        // Arrange
        var rules = new List<ILogSamplingFilterRule>
        {
            new ProbabilisticSamplerFilterRule(probability : 0, logLevel : LogLevel.Information, eventId : 1),
            new ProbabilisticSamplerFilterRule(probability : 0, logLevel : LogLevel.Information, eventId : 1),
            new ProbabilisticSamplerFilterRule(probability: 0, logLevel: LogLevel.Warning),
            new ProbabilisticSamplerFilterRule(probability : 0, logLevel : LogLevel.Warning, eventId : 2),
            new ProbabilisticSamplerFilterRule(probability : 0, logLevel : LogLevel.Warning, eventId : 1),
            new ProbabilisticSamplerFilterRule(probability : 0, categoryName : "Program1.MyLogger", logLevel : LogLevel.Warning, eventId : 1),
            new ProbabilisticSamplerFilterRule(probability : 0, categoryName : "Program.*MyLogger1", logLevel : LogLevel.Warning, eventId : 1),
            new ProbabilisticSamplerFilterRule(probability : 0, categoryName : "Program.MyLogger", logLevel : LogLevel.Warning, eventId : 1), // the best rule
            new ProbabilisticSamplerFilterRule(probability: 0, categoryName: "Program.MyLogger*", logLevel: LogLevel.Warning, eventId: 1), // same as the best, but last, and should be selected
        };

        // Act
        LogSamplingRuleSelector.Select(rules, "Program.MyLogger", LogLevel.Warning, 1, out var actualResult);

        // Assert
        Assert.Same(rules.Last(), actualResult);
    }
}
