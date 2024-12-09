// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics;

/// <summary>
/// Represents a rule used for filtering log messages for purposes of log sampling and buffering.
/// </summary>
internal interface ILoggerFilterRule
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
}
