// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public string? CategoryName { get; }

    /// <summary>
    /// Gets the maximum <see cref="LogLevel"/> of messages.
    /// </summary>
    public LogLevel? LogLevel { get; }

    /// <summary>
    /// Gets the <see cref="EventId"/> of messages this rule applies to.
    /// </summary>
    public EventId? EventId { get; }
}
