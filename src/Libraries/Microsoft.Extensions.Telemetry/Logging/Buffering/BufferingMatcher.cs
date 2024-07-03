// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.Logging.Sampling;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;

/// <summary>
/// A log pattern matcher.
/// </summary>
public class BufferingMatcher : IMatcher
{
    private readonly LogRecordPattern _pattern;

    /// <summary>
    /// Gets a buffering delegate.
    /// </summary>
    public Action<LogBuffer, LogRecordPattern>? Buffer { get; }

    /// <summary>
    /// Gets a control action to perform in case there is a match.
    /// </summary>
    public ControlAction ControlAction { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BufferingMatcher"/> class.
    /// </summary>
    /// <param name="pattern">A log record pattern to match.</param>
    /// <param name="buffer">A buffering delegate.</param>
    public BufferingMatcher(LogRecordPattern pattern, Action<LogBuffer, LogRecordPattern> buffer)
    {
        ControlAction = ControlAction.GlobalBuffer;
        _pattern = pattern;
        Buffer = buffer;
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
