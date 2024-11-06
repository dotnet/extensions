// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// Represents a rule used for filtering log messages for purposes of log sampling and buffering.
/// </summary>
public interface ILoggerSamplerFilterRule
{
    /// <summary>
    /// Gets the logger category this rule applies to.
    /// </summary>
    public string? Category { get; }

    /// <summary>
    /// Gets the maximum <see cref="LogLevel"/> of messages.
    /// </summary>
    public LogLevel? LogLevel { get; }

    /// <summary>
    /// Gets the maximum <see cref="LogLevel"/> of messages where this rule applies to.
    /// </summary>
    public int? EventId { get; }

    /// <summary>
    /// Gets the filter delegate that would be applied to messages that passed the <see cref="LogLevel"/>.
    /// </summary>
    public Func<string?, LogLevel?, int?, bool>? Filter { get; }
}
