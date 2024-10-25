// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Contains the parameters helping make sampling decisions for logs.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public readonly struct SamplingParameters : IEquatable<SamplingParameters>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SamplingParameters"/> struct.
    /// </summary>
    /// <param name="logLevel"><see cref="Microsoft.Extensions.Logging.LogLevel"/> of the log record.</param>
    /// <param name="category">Category of the log record.</param>
    /// <param name="eventId"><see cref="Microsoft.Extensions.Logging.EventId"/> of the log record.</param>
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

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is SamplingParameters samplingParameters && Equals(samplingParameters);

    /// <inheritdoc/>
    public bool Equals(SamplingParameters other)
    {
        return other.EventId.Equals(EventId)
            && string.Equals(other.Category, Category, StringComparison.Ordinal)
            && other.LogLevel.Equals(LogLevel);
    }

    /// <inheritdoc/>
    public override int GetHashCode() =>

        HashCode.Combine(
            LogLevel.GetHashCode(),
#if NETFRAMEWORK
            Category?.GetHashCode(),
#else
            Category?.GetHashCode(StringComparison.Ordinal),
#endif
            EventId.GetHashCode());

    /// <summary>
    /// Checks if two specified <see cref="SamplingParameters"/> instances have the same value.
    /// They are equal if their respective properties have the same values.
    /// </summary>
    /// <param name="left">The first <see cref="SamplingParameters"/>.</param>
    /// <param name="right">The second <see cref="SamplingParameters"/>.</param>
    /// <returns><see langword="true" /> if the objects are equal.</returns>
    public static bool operator ==(SamplingParameters left, SamplingParameters right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Checks if two specified <see cref="SamplingParameters"/> instances have different values.
    /// </summary>
    /// <param name="left">The first <see cref="SamplingParameters"/>.</param>
    /// <param name="right">The second <see cref="SamplingParameters"/>.</param>
    /// <returns><see langword="true" /> if the objects are not equal.</returns>
    public static bool operator !=(SamplingParameters left, SamplingParameters right)
    {
        return !left.Equals(right);
    }
}
