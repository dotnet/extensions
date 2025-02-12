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
            new ProbabilisticSamplerFilterRule { Probability = 0 },
            new ProbabilisticSamplerFilterRule { Probability = 0, EventId = 1 },
            new ProbabilisticSamplerFilterRule { Probability = 0, LogLevel = LogLevel.Information, EventId = 1 },
            new ProbabilisticSamplerFilterRule { Probability = 0, LogLevel = LogLevel.Information, EventId = 1 },
            new ProbabilisticSamplerFilterRule { Probability = 0, LogLevel = LogLevel.Warning },
            new ProbabilisticSamplerFilterRule { Probability = 0, LogLevel = LogLevel.Warning, EventId = 2 },
            new ProbabilisticSamplerFilterRule { Probability = 0, LogLevel = LogLevel.Warning, EventId = 1 },
            new ProbabilisticSamplerFilterRule { Probability = 0, Category = "Program1.MyLogger", LogLevel = LogLevel.Warning, EventId = 1 },
            new ProbabilisticSamplerFilterRule { Probability = 0, Category = "Program.*MyLogger1", LogLevel = LogLevel.Warning, EventId = 1 },
            new ProbabilisticSamplerFilterRule { Probability = 0, Category = "Program.MyLogger", LogLevel = LogLevel.Warning, EventId = 1 }, // the best rule
            new ProbabilisticSamplerFilterRule { Probability = 0, Category = "Program.MyLogger", LogLevel = LogLevel.Warning, EventId = 2 },
            new ProbabilisticSamplerFilterRule { Probability = 0, Category = "Program.MyLogger", EventId = 1 },
            new ProbabilisticSamplerFilterRule { Probability = 0, LogLevel = LogLevel.Warning, EventId = 1 },
            new ProbabilisticSamplerFilterRule { Probability = 0, Category = "Program", LogLevel = LogLevel.Warning, EventId = 1 },
            new ProbabilisticSamplerFilterRule { Probability = 0, Category = "Program.MyLogger", LogLevel = LogLevel.Warning },
            new ProbabilisticSamplerFilterRule { Probability = 0, Category = "Program.MyLogger", LogLevel = LogLevel.Error, EventId = 1 },
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
            new ProbabilisticSamplerFilterRule { Probability = 0, LogLevel = LogLevel.Information, EventId = 1 },
            new ProbabilisticSamplerFilterRule { Probability = 0, LogLevel = LogLevel.Information, EventId = 1 },
            new ProbabilisticSamplerFilterRule { Probability = 0, LogLevel = LogLevel.Warning },
            new ProbabilisticSamplerFilterRule { Probability = 0, LogLevel = LogLevel.Warning, EventId = 2 },
            new ProbabilisticSamplerFilterRule { Probability = 0, LogLevel = LogLevel.Warning, EventId = 1 },
            new ProbabilisticSamplerFilterRule { Probability = 0, Category = "Program1.MyLogger", LogLevel = LogLevel.Warning, EventId = 1 },
            new ProbabilisticSamplerFilterRule { Probability = 0, Category = "Program.*MyLogger1", LogLevel = LogLevel.Warning, EventId = 1 },
            new ProbabilisticSamplerFilterRule { Probability = 0, Category = "Program.MyLogger", LogLevel = LogLevel.Warning, EventId = 1 }, // the best rule
            new ProbabilisticSamplerFilterRule { Probability = 0, Category = "Program.MyLogger*", LogLevel = LogLevel.Warning, EventId = 1 }, // same as the best, but last, and should be selected
        };

        // Act
        SamplerRuleSelector.Select(rules, "Program.MyLogger", LogLevel.Warning, 1, out var actualResult);

        // Assert
        Assert.Same(rules.Last(), actualResult);
    }
}
