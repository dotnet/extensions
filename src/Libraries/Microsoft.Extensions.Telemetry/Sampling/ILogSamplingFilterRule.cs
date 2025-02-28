// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// Represents a rule used for filtering log messages for purposes of log sampling and buffering.
/// </summary>
internal interface ILogSamplingFilterRule
{
    /// <summary>
    /// Gets the logger category this rule applies to.
    /// </summary>
    string? CategoryName { get; }

    /// <summary>
    /// Gets the maximum <see cref="LogLevel"/> of messages.
    /// </summary>
    LogLevel? LogLevel { get; }

    /// <summary>
    /// Gets the event ID this rule applies to.
    /// </summary>
    int? EventId { get; }

    /// <summary>
    /// Gets the name of the event this rule applies to.
    /// </summary>
    string? EventName { get; }
}
