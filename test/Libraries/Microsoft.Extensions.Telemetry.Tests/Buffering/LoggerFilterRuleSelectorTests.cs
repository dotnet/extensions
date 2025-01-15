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
        var rules = new List<ILoggerFilterRule>
        {
            new BufferFilterRule(null, null, null, null),
            new BufferFilterRule(null, null, 1, null),
            new BufferFilterRule(null, LogLevel.Information, 1, null),
            new BufferFilterRule(null, LogLevel.Information, 1, null),
            new BufferFilterRule(null, LogLevel.Warning, null, null),
            new BufferFilterRule(null, LogLevel.Warning, 2, null),
            new BufferFilterRule(null, LogLevel.Warning, 1, null),
            new BufferFilterRule("Program1.MyLogger", LogLevel.Warning, 1, null),
            new BufferFilterRule("Program.*MyLogger1", LogLevel.Warning, 1, null),
            new BufferFilterRule("Program.MyLogger", LogLevel.Warning, 1), // the best rule
            new BufferFilterRule("Program.MyLogger", LogLevel.Warning, 2, null),
            new BufferFilterRule("Program.MyLogger", null, 1, null),
            new BufferFilterRule(null, LogLevel.Warning, 1, null),
            new BufferFilterRule("Program", LogLevel.Warning, 1, null),
            new BufferFilterRule("Program.MyLogger", LogLevel.Warning, null, null),
            new BufferFilterRule("Program.MyLogger", LogLevel.Error, 1, null),
        };

        // Act
        LoggerFilterRuleSelector.Select(
            rules, "Program.MyLogger", LogLevel.Warning, 1, out var actualResult);

        // Assert
        Assert.Same(rules[9], actualResult);
    }

    [Fact]
    public void WhenManyRuleApply_SelectsLast()
    {
        // Arrange
        var rules = new List<ILoggerFilterRule>
        {
            new BufferFilterRule(null, LogLevel.Information, 1, null),
            new BufferFilterRule(null, LogLevel.Information, 1, null),
            new BufferFilterRule(null, LogLevel.Warning, null, null),
            new BufferFilterRule(null, LogLevel.Warning, 2, null),
            new BufferFilterRule(null, LogLevel.Warning, 1, null),
            new BufferFilterRule("Program1.MyLogger", LogLevel.Warning, 1, null),
            new BufferFilterRule("Program.*MyLogger1", LogLevel.Warning, 1, null),
            new BufferFilterRule("Program.MyLogger", LogLevel.Warning, 1, null),
            new BufferFilterRule("Program.MyLogger*", LogLevel.Warning, 1, null),
            new BufferFilterRule("Program.MyLogger", LogLevel.Warning, 1), // the best rule
            new BufferFilterRule("Program.MyLogger*", LogLevel.Warning, 1), // same as the best, but last and should be selected
        };

        // Act
        LoggerFilterRuleSelector.Select(rules, "Program.MyLogger", LogLevel.Warning, 1, out var actualResult);

        // Assert
        Assert.Same(rules.Last(), actualResult);
    }
}
