// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Sampling;

internal static class LogSamplingRuleSelectorExtensions
{
    public static T? GetBestMatchFor<T>(this IList<T> rules, string category, LogLevel logLevel, EventId eventId)
        where T : class, ILogSamplingFilterRule
    {
        // Filter rule selection:
        // 0. Ignore rules whose LogLevel is defined but lower than the requested logLevel
        // 1. Ignore rules whose EventId is defined but different from the requested eventId
        // 2. For category filtering, handle optional wildcards (only one '*' allowed) and match the prefix/suffix ignoring case
        // 3. Out of the matched set, pick the rule with the longest matching category
        // 4. If no rules match by category, accept rules without a category
        // 5. If exactly one rule remains, use it; if multiple ones remain, select the last in the list
        T? current = null;
        foreach (T rule in rules)
        {
            if (LogSamplingRuleSelector<T>.IsBetter(rule, current, category, logLevel, eventId))
            {
                current = rule;
            }
        }

        return current;
    }
}
