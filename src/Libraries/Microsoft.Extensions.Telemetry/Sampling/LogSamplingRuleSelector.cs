// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1307 // Specify StringComparison for clarity
#pragma warning disable S1659 // Multiple variables should not be declared on the same line
#pragma warning disable S2302 // "nameof" should be used

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Sampling;

internal static class LogSamplingRuleSelector
{
    public static void Select<T>(IList<T> rules, string category, LogLevel logLevel, EventId eventId, out T? bestRule)
        where T : class, ILogSamplingFilterRule
    {
        bestRule = null;

        // Filter rule selection:
        // 0. Ignore rules whose LogLevel is defined but lower than the requested logLevel
        // 1. Ignore rules whose EventId is defined but different from the requested eventId
        // 2. For category filtering, handle optional wildcards (only one '*' allowed) and match the prefix/suffix ignoring case
        // 3. Out of the matched set, pick the rule with the longest matching category
        // 4. If no rules match by category, accept rules without a category
        // 5. If exactly one rule remains, use it; if multiple remain, select the last in the list

        T? current = null;
        foreach (T rule in rules)
        {
            if (IsBetter(rule, current, category, logLevel, eventId))
            {
                current = rule;
            }
        }

        if (current != null)
        {
            bestRule = current;
        }
    }

    private static bool IsBetter<T>(T rule, T? current, string category, LogLevel logLevel, EventId eventId)
        where T : class, ILogSamplingFilterRule
    {
        // Skip rules with inapplicable log level
        if (rule.LogLevel != null && rule.LogLevel < logLevel)
        {
            return false;
        }

        // Skip rules with inapplicable event id
        if (rule.EventId != null && rule.EventId != eventId)
        {
            return false;
        }

        // Skip rules with inapplicable category
        string? categoryName = rule.CategoryName;
        if (categoryName != null)
        {
            const char WildcardChar = '*';

            int wildcardIndex = categoryName.IndexOf(WildcardChar);
            if (wildcardIndex != -1 &&
                categoryName.IndexOf(WildcardChar, wildcardIndex + 1) != -1)
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

        // Decide whose category is better - rule vs current
        if (current?.CategoryName != null)
        {
            if (rule.CategoryName == null)
            {
                return false;
            }

            if (current.CategoryName.Length > rule.CategoryName.Length)
            {
                return false;
            }
        }

        // Decide whose log level is better - rule vs current
        if (current?.LogLevel != null)
        {
            if (rule.LogLevel == null)
            {
                return false;
            }

            if (current.LogLevel < rule.LogLevel)
            {
                return false;
            }
        }

        // Decide whose event id is better - rule vs current
        if (rule.EventId is null)
        {
            if (current?.EventId != null)
            {
                return false;
            }
        }

        return true;
    }
}
