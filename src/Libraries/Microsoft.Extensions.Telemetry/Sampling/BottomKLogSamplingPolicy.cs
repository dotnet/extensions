// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System.Collections.Frozen;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Sampling;

internal sealed class BottomKLogSamplingPolicy
{
    private readonly BottomKCategoryMatcher _retainAllCategories;
    private readonly FrozenSet<int> _retainAllEventIds;
    private readonly FrozenSet<LogLevel> _retainAllLogLevels;
    private readonly BottomKCategoryMatcher _sampledCategories;

    public BottomKLogSamplingPolicy(BottomKLogSamplingOptions options)
    {
        Options = options;
        _retainAllLogLevels = options.RetainAllLogLevels.ToFrozenSet();
        _retainAllEventIds = options.RetainAllEventIds.ToFrozenSet();
        _retainAllCategories = new BottomKCategoryMatcher(options.RetainAllCategories);
        _sampledCategories = new BottomKCategoryMatcher(options.SampledCategories);
    }

    public BottomKLogSamplingOptions Options { get; }

    public bool ShouldBypass(string category, LogLevel logLevel, EventId eventId)
    {
        if (!Options.Enabled
            || _retainAllLogLevels.Contains(logLevel)
            || _retainAllEventIds.Contains(eventId.Id)
            || _retainAllCategories.Matches(category))
        {
            return true;
        }

        return !_sampledCategories.IsEmpty && !_sampledCategories.Matches(category);
    }
}
#endif
