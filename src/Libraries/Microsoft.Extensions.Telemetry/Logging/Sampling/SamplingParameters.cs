// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// Contains the parameters used for sampling logs.
/// </summary>
public readonly struct SamplingParameters
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SamplingParameters"/> struct.
    /// </summary>
    /// <param name="category">cat.</param>
    /// <param name="eventId">event id.</param>
    /// <param name="logLevel">level.</param>
    public SamplingParameters(LogLevel? logLevel, string? category, EventId? eventId)
    {
        LogLevel = logLevel;
        Category = category;
        EventId = eventId;
    }

    /// <summary>
    /// Gets the log category.
    /// </summary>
    public string? Category { get; }

    /// <summary>
    /// Gets the event ID.
    /// </summary>
    public EventId? EventId { get; }

    /// <summary>
    /// Gets the log level.
    /// </summary>
    public LogLevel? LogLevel { get; }
}
