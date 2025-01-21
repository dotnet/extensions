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
        var rules = new List<BufferFilterRule>
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
            new BufferFilterRule("Program.MyLogger", LogLevel.Warning, 1, [new("region2", "westus2")]), // inapplicable key
            new BufferFilterRule("Program.MyLogger", LogLevel.Warning, 1, [new("region", "westus3")]), // inapplicable value
            new BufferFilterRule("Program.MyLogger", LogLevel.Warning, 1, [new("region", "westus2")]), // the best rule - [11]
            new BufferFilterRule("Program.MyLogger", LogLevel.Warning, 2, null),
            new BufferFilterRule("Program.MyLogger", null, 1, null),
            new BufferFilterRule(null, LogLevel.Warning, 1, null),
            new BufferFilterRule("Program", LogLevel.Warning, 1, null),
            new BufferFilterRule("Program.MyLogger", LogLevel.Warning, null, null),
            new BufferFilterRule("Program.MyLogger", LogLevel.Error, 1, null),
        };

        // Act
        BufferFilterRuleSelector.Select(
            rules, "Program.MyLogger", LogLevel.Warning, 1, [new("region", "westus2")], out var actualResult);

        // Assert
        Assert.Same(rules[11], actualResult);
    }

    [Fact]
    public void WhenManyRuleApply_SelectsLast()
    {
        // Arrange
        var rules = new List<BufferFilterRule>
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
            new BufferFilterRule("Program.MyLogger", LogLevel.Warning, 1, [new("region", "westus2")]), // the best rule
            new BufferFilterRule("Program.MyLogger*", LogLevel.Warning, 1, [new("region", "westus2")]), // same as the best, but last and should be selected
        };

        // Act
        BufferFilterRuleSelector.Select(rules, "Program.MyLogger", LogLevel.Warning, 1, [new("region", "westus2")], out var actualResult);

        // Assert
        Assert.Same(rules.Last(), actualResult);
    }
}
