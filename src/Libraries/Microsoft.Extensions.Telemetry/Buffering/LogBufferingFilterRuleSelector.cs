// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

#pragma warning disable CA1307 // Specify StringComparison for clarity
#pragma warning disable S1659 // Multiple variables should not be declared on the same line
#pragma warning disable S2302 // "nameof" should be used

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// Selects the best rule from the list of rules for a given log event.
/// </summary>
internal sealed class LogBufferingFilterRuleSelector
{
    private static readonly IEqualityComparer<KeyValuePair<string, object?>> _stringifyComparer = new StringifyComprarer();
    private static readonly ObjectPool<List<LogBufferingFilterRule>> _rulePool =
        PoolFactory.CreateListPool<LogBufferingFilterRule>();

    private readonly ObjectPool<List<LogBufferingFilterRule>> _cachedRulePool =
        PoolFactory.CreateListPool<LogBufferingFilterRule>();
    private readonly ConcurrentDictionary<(LogLevel, EventId), List<LogBufferingFilterRule>> _ruleCache = new();

    public static LogBufferingFilterRule[] SelectByCategory(IList<LogBufferingFilterRule> rules, string category)
    {
        List<LogBufferingFilterRule> rulesOfCategory = _rulePool.Get();
        try
        {
            // Select rules with applicable category only
            foreach (LogBufferingFilterRule rule in rules)
            {
                if (IsMatch(rule, category))
                {
                    rulesOfCategory.Add(rule);
                }
            }

            return rulesOfCategory.ToArray();
        }
        finally
        {
            _rulePool.Return(rulesOfCategory);
        }
    }

    public void InvalidateCache()
    {
        foreach (((LogLevel, EventId) key, List<LogBufferingFilterRule> value) in _ruleCache)
        {
            _cachedRulePool.Return(value);
        }

        _ruleCache.Clear();
    }

    public LogBufferingFilterRule? Select(
        IList<LogBufferingFilterRule> rules,
        LogLevel logLevel,
        EventId eventId,
        IReadOnlyList<KeyValuePair<string, object?>>? attributes)
    {
        // 1. select rule candidates by log level and event id from the cache
        List<LogBufferingFilterRule> ruleCandidates = _ruleCache.GetOrAdd((logLevel, eventId), _ =>
        {
            List<LogBufferingFilterRule> candidates = _cachedRulePool.Get();
            foreach (LogBufferingFilterRule rule in rules)
            {
                if (IsMatch(rule, logLevel, eventId))
                {
                    candidates.Add(rule);
                }
            }

            return candidates;
        });

        // 2. select the best rule from the candidates by attributes
        LogBufferingFilterRule? currentBest = null;
        foreach (LogBufferingFilterRule ruleCandidate in ruleCandidates)
        {
            if (IsAttributesMatch(ruleCandidate, attributes) && IsBetter(currentBest, ruleCandidate))
            {
                currentBest = ruleCandidate;
            }
        }

        return currentBest;
    }

    private static bool IsAttributesMatch(LogBufferingFilterRule rule, IReadOnlyList<KeyValuePair<string, object?>>? attributes)
    {
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

        return true;
    }

    private static bool IsBetter(LogBufferingFilterRule? currentBest, LogBufferingFilterRule ruleCandidate)
    {
        // Decide whose attributes are better - rule vs current
        if (currentBest?.Attributes?.Count > 0)
        {
            if (ruleCandidate.Attributes is null || ruleCandidate.Attributes.Count == 0)
            {
                return false;
            }

            if (ruleCandidate.Attributes.Count < currentBest.Attributes.Count)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsMatch(LogBufferingFilterRule rule, string category)
    {
        const char WildcardChar = '*';

        string? ruleCategory = rule.CategoryName;
        if (ruleCategory is null)
        {
            return true;
        }

        int wildcardIndex = ruleCategory.IndexOf(WildcardChar);

        ReadOnlySpan<char> prefix, suffix;
        if (wildcardIndex == -1)
        {
            prefix = ruleCategory.AsSpan();
            suffix = default;
        }
        else
        {
            prefix = ruleCategory.AsSpan(0, wildcardIndex);
            suffix = ruleCategory.AsSpan(wildcardIndex + 1);
        }

        if (!category.AsSpan().StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            !category.AsSpan().EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool IsMatch(LogBufferingFilterRule rule, LogLevel logLevel, EventId eventId)
    {
        // Skip rules with inapplicable log level
        if (rule.LogLevel is not null && rule.LogLevel < logLevel)
        {
            return false;
        }

        // Skip rules with inapplicable event id
        if (rule.EventId is not null && rule.EventId != eventId)
        {
            return false;
        }

        return true;
    }
}

#endif
