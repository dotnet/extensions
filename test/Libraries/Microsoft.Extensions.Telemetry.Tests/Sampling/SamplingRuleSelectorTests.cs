// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Sampling;
public class SamplingRuleSelectorTests
{
    [Fact]
    public void SelectsRightRule()
    {
        // Arrange
        var rules = new List<ILoggerSamplerFilterRule>
        {
            new ProbabilitySamplerFilterRule(0, null, null, null),
            new ProbabilitySamplerFilterRule(0, null, null, 1),
            new ProbabilitySamplerFilterRule(0, null, LogLevel.Information, 1),
            new ProbabilitySamplerFilterRule(0, null, LogLevel.Information, 1),
            new ProbabilitySamplerFilterRule(0, null, LogLevel.Warning, null),
            new ProbabilitySamplerFilterRule(0, null, LogLevel.Warning, 2),
            new ProbabilitySamplerFilterRule(0, null, LogLevel.Warning, 1),
            new ProbabilitySamplerFilterRule(0, "Program1.MyLogger", LogLevel.Warning, 1),
            new ProbabilitySamplerFilterRule(0, "Program.*MyLogger1", LogLevel.Warning, 1),
            new ProbabilitySamplerFilterRule(0, "Program.MyLogger", LogLevel.Warning, 1), // the best rule
            new ProbabilitySamplerFilterRule(0, "Program.MyLogger", LogLevel.Warning, 2),
            new ProbabilitySamplerFilterRule(0, "Program.MyLogger", null, 1),
            new ProbabilitySamplerFilterRule(0, null, LogLevel.Warning, 1),
            new ProbabilitySamplerFilterRule(0, "Program", LogLevel.Warning, 1),
            new ProbabilitySamplerFilterRule(0, "Program.MyLogger", LogLevel.Warning, null),
            new ProbabilitySamplerFilterRule(0, "Program.MyLogger", LogLevel.Error, 1),
        };

        // Act
        SamplerRuleSelector.Select(rules, "Program.MyLogger", LogLevel.Warning, 1, out var actualResult);

        // Assert
        Assert.Same(rules[9], actualResult);
    }

    [Fact]
    public void WhenManyRuleApply_SelectsLast()
    {
        // Arrange
        var rules = new List<ILoggerSamplerFilterRule>
        {
            new ProbabilitySamplerFilterRule(0, null, LogLevel.Information, 1),
            new ProbabilitySamplerFilterRule(0, null, LogLevel.Information, 1),
            new ProbabilitySamplerFilterRule(0, null, LogLevel.Warning, null),
            new ProbabilitySamplerFilterRule(0, null, LogLevel.Warning, 2),
            new ProbabilitySamplerFilterRule(0, null, LogLevel.Warning, 1),
            new ProbabilitySamplerFilterRule(0, "Program1.MyLogger", LogLevel.Warning, 1),
            new ProbabilitySamplerFilterRule(0, "Program.*MyLogger1", LogLevel.Warning, 1),
            new ProbabilitySamplerFilterRule(0, "Program.MyLogger", LogLevel.Warning, 1), // the best rule
            new ProbabilitySamplerFilterRule(0, "Program.MyLogger*", LogLevel.Warning, 1), // same as the best, but last, and should be selected
        };

        // Act
        SamplerRuleSelector.Select(rules, "Program.MyLogger", LogLevel.Warning, 1, out var actualResult);

        // Assert
        Assert.Same(rules.Last(), actualResult);
    }
}
