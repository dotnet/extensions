// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1307 // Specify StringComparison for clarity
#pragma warning disable S1659 // Multiple variables should not be declared on the same line
#pragma warning disable S2302 // "nameof" should be used

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Sampling;

internal sealed class LogSamplingRuleSelector<T>
    where T : class, ILogSamplingFilterRule
{
    private readonly ConcurrentDictionary<(string, LogLevel, EventId), T?> _ruleCache = new();

    public static bool IsBetter(T rule, T? current, string category, LogLevel logLevel, EventId eventId)
    {
        // Skip rules with an inapplicable log level
        if (rule.LogLevel is not null && rule.LogLevel < logLevel)
        {
            return false;
        }

        // Skip rules with inapplicable event id
        if (rule.EventId is not null && rule.EventId != eventId)
        {
            return false;
        }

        // Skip rules with inapplicable category
        string? categoryName = rule.CategoryName;
        if (categoryName is not null)
        {
            const char WildcardChar = '*';

            int wildcardIndex = categoryName.IndexOf(WildcardChar);
            if (wildcardIndex >= 0 &&
                categoryName.IndexOf(WildcardChar, wildcardIndex + 1) >= 0)
            {
                throw new InvalidOperationException("Only one wildcard character is allowed in category name.");
            }

            ReadOnlySpan<char> prefix, suffix;
            if (wildcardIndex == -1)
            {
                prefix = categoryName.AsSpan();
                suffix = default;
            }
            else
            {
                prefix = categoryName.AsSpan(0, wildcardIndex);
                suffix = categoryName.AsSpan(wildcardIndex + 1);
            }

            if (!category.AsSpan().StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                !category.AsSpan().EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Decide whose category is better - rule vs. current
        if (current?.CategoryName is not null)
        {
            if (rule.CategoryName is null)
            {
                return false;
            }

            if (current.CategoryName.Length > rule.CategoryName.Length)
            {
                return false;
            }
        }

        // Decide whose log level is better - rule vs. current
        if (current?.LogLevel is not null)
        {
            if (rule.LogLevel is null)
            {
                return false;
            }

            if (current.LogLevel < rule.LogLevel)
            {
                return false;
            }
        }

        // Decide whose event id is better - rule vs. current
        if (rule.EventId is null)
        {
            if (current?.EventId is not null)
            {
                return false;
            }
        }

        return true;
    }

    public void InvalidateCache()
    {
        _ruleCache.Clear();
    }

    public T? Select(IList<T> rules, string category, LogLevel logLevel, EventId eventId)
    {
        return _ruleCache.GetOrAdd((category, logLevel, eventId), rules.GetBestMatchFor(category, logLevel, eventId));
    }
}
