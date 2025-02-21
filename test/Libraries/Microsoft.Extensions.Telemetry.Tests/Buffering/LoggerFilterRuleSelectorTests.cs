// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Diagnostics.Buffering;
using Xunit;

namespace Microsoft.Extensions.Logging.Test;
public class LoggerFilterRuleSelectorTests
{
    [Fact]
    public void SelectsRightRule()
    {
        // Arrange
        var rules = new List<LogBufferingFilterRule>
        {
            new LogBufferingFilterRule(null, null, null, null),
            new LogBufferingFilterRule(null, null, 1, null),
            new LogBufferingFilterRule(null, LogLevel.Information, 1, null),
            new LogBufferingFilterRule(null, LogLevel.Information, 1, null),
            new LogBufferingFilterRule(null, LogLevel.Warning, null, null),
            new LogBufferingFilterRule(null, LogLevel.Warning, 2, null),
            new LogBufferingFilterRule(null, LogLevel.Warning, 1, null),
            new LogBufferingFilterRule("Program1.MyLogger", LogLevel.Warning, 1, null),
            new LogBufferingFilterRule("Program.*MyLogger1", LogLevel.Warning, 1, null),
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 1, attributes: [new("region2", "westus2")]), // inapplicable key
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 1, attributes:[new("region", "westus3")]), // inapplicable value
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 1, attributes:[new("region", "westus2")]), // the best rule - [11]
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 2, null),
            new LogBufferingFilterRule("Program.MyLogger", null, 1, null),
            new LogBufferingFilterRule(null, LogLevel.Warning, 1, null),
            new LogBufferingFilterRule("Program", LogLevel.Warning, 1, null),
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, null, null),
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Error, 1, null),
        };

        // Act
        LogBufferingFilterRuleSelector.Select(
            rules, "Program.MyLogger", LogLevel.Warning, 1, [new("region", "westus2")], out var actualResult);

        // Assert
        Assert.Same(rules[11], actualResult);
    }

    [Fact]
    public void WhenManyRuleApply_SelectsLast()
    {
        // Arrange
        var rules = new List<LogBufferingFilterRule>
        {
            new LogBufferingFilterRule(null, LogLevel.Information, 1, null),
            new LogBufferingFilterRule(null, LogLevel.Information, 1, null),
            new LogBufferingFilterRule(null, LogLevel.Warning, null, null),
            new LogBufferingFilterRule(null, LogLevel.Warning, 2, null),
            new LogBufferingFilterRule(null, LogLevel.Warning, 1, null),
            new LogBufferingFilterRule("Program1.MyLogger", LogLevel.Warning, 1, null),
            new LogBufferingFilterRule("Program.*MyLogger1", LogLevel.Warning, 1, null),
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 1, null),
            new LogBufferingFilterRule("Program.MyLogger*", LogLevel.Warning, 1, null),
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 1, attributes:[new("region", "westus2")]), // the best rule
            new LogBufferingFilterRule("Program.MyLogger*", LogLevel.Warning, 1, attributes:[new("region", "westus2")]), // same as the best, but last and should be selected
        };

        // Act
        LogBufferingFilterRuleSelector.Select(rules, "Program.MyLogger", LogLevel.Warning, 1, [new("region", "westus2")], out var actualResult);

        // Assert
        Assert.Same(rules.Last(), actualResult);
    }

    [Fact]
    public void CanWorkWithValueTypeAttributes()
    {
        // Arrange
        var rules = new List<LogBufferingFilterRule>
        {
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 1, attributes:[new("priority", 1)]),
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 1, attributes:[new("priority", 2)]), // the best rule
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 1, attributes:[new("priority", 3)]),
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 1, null),
        };

        // Act
        LogBufferingFilterRuleSelector.Select(rules, "Program.MyLogger", LogLevel.Warning, 1, [new("priority", "2")], out var actualResult);

        // Assert
        Assert.Same(rules[1], actualResult);
    }
}
