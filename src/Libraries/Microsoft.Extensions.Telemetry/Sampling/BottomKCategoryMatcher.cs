// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.Sampling;

internal sealed class BottomKCategoryMatcher
{
    private readonly FrozenSet<string> _exactCategories;
    private readonly string[] _wildcardCategories;

    public BottomKCategoryMatcher(IList<string> categories)
    {
        var exactCategories = new List<string>(categories.Count);
        var wildcardCategories = new List<string>();
        for (int i = 0; i < categories.Count; i++)
        {
            string category = categories[i];
            if (category.Contains('*', StringComparison.Ordinal))
            {
                wildcardCategories.Add(category);
            }
            else
            {
                exactCategories.Add(category);
            }
        }

        _exactCategories = exactCategories.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _wildcardCategories = wildcardCategories.ToArray();
    }

    public bool IsEmpty => _exactCategories.Count == 0 && _wildcardCategories.Length == 0;

    public bool Matches(string category)
    {
        if (_exactCategories.Contains(category))
        {
            return true;
        }

        for (int i = 0; i < _wildcardCategories.Length; i++)
        {
            if (MatchesWildcard(category, _wildcardCategories[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesWildcard(string category, string pattern)
    {
        int wildcard = pattern.IndexOf("*", StringComparison.Ordinal);
        return category.Length >= pattern.Length - 1
            && category.AsSpan().StartsWith(pattern.AsSpan(0, wildcard), StringComparison.OrdinalIgnoreCase)
            && category.AsSpan().EndsWith(pattern.AsSpan(wildcard + 1), StringComparison.OrdinalIgnoreCase);
    }
}
#endif
