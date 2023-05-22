// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Telemetry.Metering.Internal;

#pragma warning disable S109 // Magic numbers should not be used
internal static partial class Log
{
    /// <summary>
    /// Logs `Configured EventCounters options: {value}` at `Information` level.
    /// </summary>
    [LogMethod(0, LogLevel.Information, "Configured EventCounters options: {value}")]
    internal static partial void ConfiguredEventCountersOptions(this ILogger logger, EventCountersCollectorOptions value);

    /// <summary>
    /// Logs `Enabling event source: {name}` at `Information` level.
    /// </summary>
    [LogMethod(1, LogLevel.Information, "Enabling event source: {name}")]
    internal static partial void EnablingEventSource(this ILogger logger, string name);

    /// <summary>
    /// Logs `Invalid value {value} resulting in overflow exception during conversion` at `Warning` level.
    /// </summary>
    [LogMethod(2, LogLevel.Warning, "Invalid value {value} resulting in overflow exception during conversion")]
    internal static partial void OverflowExceptionWhileConversion(this ILogger logger, object? value);

    /// <summary>
    /// Logs `Invalid value {value} resulting in Format exception during conversion` at `Warning` level.
    /// </summary>
    [LogMethod(3, LogLevel.Warning, "Invalid value {value} resulting in Format exception during conversion")]
    internal static partial void FormatExceptionWhileConversion(this ILogger logger, object? value);

    /// <summary>
    /// Logs `Event name is null, eventSource {name}` at `Debug` level.
    /// </summary>
    [LogMethod(4, LogLevel.Debug, "Event name is null, eventSource {name}")]
    internal static partial void EventNameIsNull(this ILogger logger, string name);

    /// <summary>
    /// Logs `Payload is null for event {name}:{eventName}` at `Debug` level.
    /// </summary>
    [LogMethod(5, LogLevel.Debug, "Payload is null for event {eventSourceName}:{eventName}")]
    internal static partial void PayloadIsNull(this ILogger logger, string eventSourceName, string eventName);

    /// <summary>
    /// Logs `EventName: `{eventName}` is not `EventCounters`` at `Debug` level.
    /// </summary>
    [LogMethod(6, LogLevel.Debug, "EventName: `{eventName}` is not `EventCounters`")]
    internal static partial void EventNameIsNotEventCounters(this ILogger logger, string eventName);

    /// <summary>
    /// Logs `No counters registered for eventSource: {eventSourceName}` at `Debug` level.
    /// </summary>
    [LogMethod(7, LogLevel.Debug, "No counters registered for eventSource: {eventSourceName}")]
    internal static partial void NoCountersRegisteredForEventSource(this ILogger logger, string eventSourceName);

    /// <summary>
    /// Logs `Event payload for event {eventSourceName}:{eventName} does not contain `CounterType`` at `Trace` level.
    /// </summary>
    [LogMethod(8, LogLevel.Trace, "Event payload for event {eventSourceName}:{eventName} does not contain `CounterType`")]
    internal static partial void EventPayloadDoesNotContainCounterType(this ILogger logger, string eventSourceName, string eventName);

    /// <summary>
    /// Logs `Event payload for event {eventSourceName}:{eventName} does not contain `Name`` at `Trace` level.
    /// </summary>
    [LogMethod(9, LogLevel.Trace, "Event payload for event {eventSourceName}:{eventName} does not contain `Name`")]
    internal static partial void EventPayloadDoesNotContainName(this ILogger logger, string eventSourceName, string eventName);

    /// <summary>
    /// Logs `Counter name is empty for event {eventSourceName}:{eventName}` at `Trace` level.
    /// </summary>
    [LogMethod(10, LogLevel.Trace, "Counter name is empty for event {eventSourceName}:{eventName}")]
    internal static partial void CounterNameIsEmpty(this ILogger logger, string eventSourceName, string eventName);

    /// <summary>
    /// Logs `Counter `{counterName}` for eventSource {eventSourceName}:{eventName} not enabled` at `Trace` level.
    /// </summary>
    [LogMethod(11, LogLevel.Trace, "Counter `{counterName}` for eventSource {eventSourceName}:{eventName} not enabled")]
    internal static partial void CounterNotEnabled(this ILogger logger, string counterName, string eventSourceName, string eventName);

    /// <summary>
    /// Logs `Received event {eventSourceName}:{counterName} of type {type}` at `Trace` level.
    /// </summary>
    [LogMethod(12, LogLevel.Trace, "Received event {eventSourceName}:{counterName} of type {type}")]
    internal static partial void ReceivedEventOfType(this ILogger logger, string eventSourceName, object counterName, string? type);
}
