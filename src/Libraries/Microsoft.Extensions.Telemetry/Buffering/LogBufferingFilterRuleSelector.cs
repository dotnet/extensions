// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1307 // Specify StringComparison for clarity
#pragma warning disable S1659 // Multiple variables should not be declared on the same line
#pragma warning disable S2302 // "nameof" should be used

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// Selects the best rule from the list of rules for a given log event.
/// </summary>
internal static class LogBufferingFilterRuleSelector
{
    private static readonly IEqualityComparer<KeyValuePair<string, object?>> _stringifyComparer = new StringifyComprarer();

    /// <summary>
    /// Selects the best rule from the list of rules for a given log event.
    /// </summary>
    /// <param name="rules">The list of rules to select from.</param>
    /// <param name="category">The category of the log event.</param>
    /// <param name="logLevel">The log level of the log event.</param>
    /// <param name="eventId">The event id of the log event.</param>
    /// <param name="attributes">The log state attributes of the log event.</param>
    /// <param name="bestRule">The best rule that matches the log event.</param>
    public static void Select(IList<LogBufferingFilterRule> rules, string category, LogLevel logLevel,
        EventId eventId, IReadOnlyList<KeyValuePair<string, object?>>? attributes, out LogBufferingFilterRule? bestRule)
    {
        bestRule = null;

        // TO DO: update the comment and logic 
        // Filter rule selection:
        // 1. Select rules with longest matching categories
        // 2. If there is nothing matched by category take all rules without category
        // 3. If there is only one rule use it
        // 4. If there are multiple rules use last

        LogBufferingFilterRule? current = null;
        if (rules is not null)
        {
            foreach (LogBufferingFilterRule rule in rules)
            {
                if (IsBetter(rule, current, category, logLevel, eventId, attributes))
                {
                    current = rule;
                }
            }
        }

        if (current != null)
        {
            bestRule = current;
        }
    }

    private static bool IsBetter(LogBufferingFilterRule rule, LogBufferingFilterRule? current, string category,
        LogLevel logLevel, EventId eventId, IReadOnlyList<KeyValuePair<string, object?>>? attributes)
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

        // Skip rules with inapplicable attributes
        if (rule.Attributes?.Count > 0 && attributes?.Count > 0)
        {
            foreach (KeyValuePair<string, object?> ruleAttribute in rule.Attributes)
            {
                if (!attributes.Contains(ruleAttribute, _stringifyComparer))
                {
                    return false;
                }
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

        // Decide whose attributes are better - rule vs current
        if (current?.Attributes?.Count > 0)
        {
            if (rule.Attributes is null || rule.Attributes.Count == 0)
            {
                return false;
            }

            if (rule.Attributes.Count < current.Attributes.Count)
            {
                return false;
            }
        }

        return true;
    }
}
