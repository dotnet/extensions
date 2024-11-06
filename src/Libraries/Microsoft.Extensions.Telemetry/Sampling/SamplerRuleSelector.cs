// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1307 // Specify StringComparison for clarity
#pragma warning disable S1659 // Multiple variables should not be declared on the same line
#pragma warning disable S2302 // "nameof" should be used

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.Sampling;

namespace Microsoft.Extensions.Logging
{
    internal static class SamplerRuleSelector
    {
        public static void Select<T>(IList<T> rules, string category, LogLevel logLevel, EventId eventId,
            out T? bestRule)
            where T : class, ILoggerSamplerFilterRule
        {
            bestRule = null;

            // Filter rule selection:
            // 1. Select rules with longest matching categories
            // 2. If there nothing matched by category take all rules without category
            // 3. If there is only one rule use it's level and filter
            // 4. If there are multiple rules use last
            // 5. If there are no applicable rules use global minimal level

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
            where T : class, ILoggerSamplerFilterRule
        {
            // Skip rules with inapplicable log level or event ID
            if (rule.LogLevel != null && rule.LogLevel < logLevel)
            {
                return false;
            }

            if (rule.EventId != null && rule.EventId != eventId)
            {
                return false;
            }

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

            return true;
        }
    }
}
