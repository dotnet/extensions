// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1307 // Specify StringComparison for clarity
#pragma warning disable S1659 // Multiple variables should not be declared on the same line
#pragma warning disable S2302 // "nameof" should be used

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Selects the best rule from the list of rules for a given log event.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public static class LoggerFilterRuleSelector
{
    /// <summary>
    /// Selects the best <typeparamref name="T"/> rule from the list of rules for a given log event.
    /// </summary>
    /// <typeparam name="T">The type of the rules.</typeparam>
    /// <param name="rules">The list of rules to select from.</param>
    /// <param name="category">The category of the log event.</param>
    /// <param name="logLevel">The log level of the log event.</param>
    /// <param name="eventId">The event id of the log event.</param>
    /// <param name="attributes">The log state attributes of the log event.</param>
    /// <param name="bestRule">The best rule that matches the log event.</param>
    public static void Select<T>(IList<T> rules, string category, LogLevel logLevel,
        EventId eventId, IReadOnlyList<KeyValuePair<string, object?>>? attributes, out T? bestRule)
        where T : class, ILoggerFilterRule
    {
        bestRule = null;

        // TO DO: update the comment and logic 
        // Filter rule selection:
        // 1. Select rules with longest matching categories
        // 2. If there is nothing matched by category take all rules without category
        // 3. If there is only one rule use it
        // 4. If there are multiple rules use last

        T? current = null;
        if (rules is not null)
        {
            foreach (T rule in rules)
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

    private static bool IsBetter<T>(T rule, T? current, string category, LogLevel logLevel, EventId eventId, IReadOnlyList<KeyValuePair<string, object?>>? attributes)
        where T : class, ILoggerFilterRule
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
        string? categoryName = rule.Category;
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
        if (rule.Attributes.Count > 0 && attributes?.Count > 0)
        {
            foreach (KeyValuePair<string, object?> ruleAttribute in rule.Attributes)
            {
                if (!attributes.Contains(ruleAttribute))
                {
                    return false;
                }
            }
        }

        // Decide whose category is better - rule vs current
        if (current?.Category != null)
        {
            if (rule.Category == null)
            {
                return false;
            }

            if (current.Category.Length > rule.Category.Length)
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
        if (current?.Attributes.Count > 0)
        {
            if (rule?.Attributes.Count == 0)
            {
                return false;
            }

            if (rule?.Attributes.Count < current.Attributes.Count)
            {
                return false;
            }
        }

        return true;
    }
}
