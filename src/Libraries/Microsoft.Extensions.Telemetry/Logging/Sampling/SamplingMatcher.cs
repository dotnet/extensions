// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// A log pattern matcher.
/// </summary>
public class SamplingMatcher : IMatcher
{
    private readonly LogRecordPattern _pattern;

    /// <summary>
    /// Gets a filtering delegate.
    /// </summary>
    public Func<LogRecordPattern, bool>? Filter { get; }

    /// <summary>
    /// Gets a control action to perform in case there is a match.
    /// </summary>
    public ControlAction ControlAction { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SamplingMatcher"/> class.
    /// </summary>
    /// <param name="pattern">A log record pattern to match.</param>
    /// <param name="filter">A filtering delegate.</param>
    public SamplingMatcher(LogRecordPattern pattern, Func<LogRecordPattern, bool> filter)
    {
        ControlAction = ControlAction.GlobalFilter;
        _pattern = pattern;
        Filter = filter;
    }

    /// <summary>
    /// Matches the log record pattern against the supplied <paramref name="pattern"/>.
    /// </summary>
    /// <param name="pattern">A log record pattern to match against.</param>
    /// <returns>True if there is a match. False otherwise.</returns>
    public bool Match(LogRecordPattern pattern)
    {
        if (_pattern.Category != null && _pattern.Category != pattern.Category)
        {
            return false;
        }

        if (_pattern.EventId != null && _pattern.EventId != pattern.EventId)
        {
            return false;
        }

        if (_pattern.LogLevel != null && _pattern.LogLevel != pattern.LogLevel)
        {
            return false;
        }

        if (_pattern.Tags != null && _pattern.Tags != pattern.Tags)
        {
            return false;
        }

        return true;
    }
}
