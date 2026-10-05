// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Buffering.Test;

public class LogBufferingFilterRuleTests
{
    private readonly LogBufferingFilterRuleSelector _selector = new();

    [Fact]
    public void SelectsRightRule()
    {
        // Arrange
        var rules = new List<LogBufferingFilterRule>
        {
            new LogBufferingFilterRule(),
            new LogBufferingFilterRule(eventId: 1),
            new LogBufferingFilterRule(logLevel: LogLevel.Information, eventId: 1),
            new LogBufferingFilterRule(logLevel: LogLevel.Information, eventId: 1),
            new LogBufferingFilterRule(logLevel: LogLevel.Warning),
            new LogBufferingFilterRule(logLevel: LogLevel.Warning, eventId: 2),
            new LogBufferingFilterRule(logLevel: LogLevel.Warning, eventId: 1),
            new LogBufferingFilterRule("Program1.MyLogger", LogLevel.Warning, 1),
            new LogBufferingFilterRule("Program.*MyLogger1", LogLevel.Warning, 1),
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 1, attributes: [new("region2", "westus2")]), // inapplicable key
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 1, attributes:[new("region", "westus3")]), // inapplicable value
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 1, attributes:[new("region", "westus2")]), // the best rule - [11]
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 2),
            new LogBufferingFilterRule("Program.MyLogger", eventId: 1),
            new LogBufferingFilterRule(logLevel: LogLevel.Warning, eventId: 1),
            new LogBufferingFilterRule("Program", LogLevel.Warning, 1),
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning),
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Error, 1),
        };

        // Act
        LogBufferingFilterRule[] categorySpecificRules = LogBufferingFilterRuleSelector.SelectByCategory(rules, "Program.MyLogger");
        LogBufferingFilterRule? result = _selector.Select(
            categorySpecificRules,
            LogLevel.Warning,
            1,
            [new("region", "westus2")]);

        // Assert
        Assert.Same(rules[11], result);
    }

    [Fact]
    public void WhenManyRuleApply_SelectsLast()
    {
        // Arrange
        var rules = new List<LogBufferingFilterRule>
        {
            new LogBufferingFilterRule(logLevel: LogLevel.Information, eventId: 1),
            new LogBufferingFilterRule(logLevel: LogLevel.Information, eventId: 1),
            new LogBufferingFilterRule(logLevel: LogLevel.Warning),
            new LogBufferingFilterRule(logLevel: LogLevel.Warning, eventId: 2),
            new LogBufferingFilterRule(logLevel: LogLevel.Warning, eventId: 1),
            new LogBufferingFilterRule("Program1.MyLogger", LogLevel.Warning, 1),
            new LogBufferingFilterRule("Program.*MyLogger1", LogLevel.Warning, 1),
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 1),
            new LogBufferingFilterRule("Program.MyLogger*", LogLevel.Warning, 1),
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 1, attributes:[new("region", "westus2")]), // the best rule
            new LogBufferingFilterRule("Program.MyLogger*", LogLevel.Warning, 1, attributes:[new("region", "westus2")]), // same as the best, but last and should be selected
        };

        // Act
        LogBufferingFilterRule[] categorySpecificRules = LogBufferingFilterRuleSelector.SelectByCategory(rules, "Program.MyLogger");
        LogBufferingFilterRule? result = _selector.Select(categorySpecificRules, LogLevel.Warning, 1, [new("region", "westus2")]);

        // Assert
        Assert.Same(rules.Last(), result);
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
            new LogBufferingFilterRule("Program.MyLogger", LogLevel.Warning, 1),
        };

        // Act
        LogBufferingFilterRule[] categorySpecificRules = LogBufferingFilterRuleSelector.SelectByCategory(rules, "Program.MyLogger");
        LogBufferingFilterRule? result = _selector.Select(categorySpecificRules, LogLevel.Warning, 1, [new("priority", "2")]);

        // Assert
        Assert.Same(rules[1], result);
    }

    [Fact]
    public void Select_IsThreadSafe_WhenCacheIsInvalidatedConcurrently()
    {
        // Arrange
        // Many candidates widen the window during which Select() enumerates the cached candidates,
        // making it likely that a concurrent InvalidateCache() call happens in the middle of the enumeration.
        var expected = new LogBufferingFilterRule(logLevel: LogLevel.Warning, eventId: 1, attributes: [new("region", "westus2")]);
        LogBufferingFilterRule[] rules =
        [
            .. Enumerable.Range(0, 100).Select(_ => new LogBufferingFilterRule(logLevel: LogLevel.Warning)),
            expected,
        ];
        KeyValuePair<string, object?>[] attributes = [new("region", "westus2")];

        // Act & Assert
        Parallel.For(0, 100_000, i =>
        {
            if (i % 10 == 0)
            {
                _selector.InvalidateCache();
            }
            else
            {
                Assert.Same(expected, _selector.Select(rules, LogLevel.Warning, 1, attributes));
            }
        });
    }
}
#endif
