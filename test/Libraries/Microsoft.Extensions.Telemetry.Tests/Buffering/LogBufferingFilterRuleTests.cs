// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public void Select_DoesNotThrow_WhenCacheIsInvalidatedDuringSelection()
    {
        // Arrange
        LogBufferingFilterRule[] rules = [new LogBufferingFilterRule(attributes: [new("region", "westus2")])];

        // Select() reads the log attributes while iterating over cached rules that have attributes. Invalidating the cache
        // at that moment reproduces another request ending in the middle of Select().
        var attributes = new AttributesWithCallback([new("region", "westus2")], onEnumerate: _selector.InvalidateCache);

        // Act
        Exception? exception = Record.Exception(() => _selector.Select(rules, LogLevel.Warning, 1, attributes));

        // Assert
        Assert.Null(exception);
    }

    private sealed class AttributesWithCallback : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private readonly KeyValuePair<string, object?>[] _attributes;
        private readonly Action _onEnumerate;

        public AttributesWithCallback(KeyValuePair<string, object?>[] attributes, Action onEnumerate)
        {
            _attributes = attributes;
            _onEnumerate = onEnumerate;
        }

        public int Count => _attributes.Length;

        public KeyValuePair<string, object?> this[int index] => _attributes[index];

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            _onEnumerate();
            return ((IEnumerable<KeyValuePair<string, object?>>)_attributes).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
#endif
