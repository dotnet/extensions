// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Telemetry.Metering;

/// <summary>
/// Configuration options for <see cref="T:Microsoft.Extensions.Telemetry.Metering.EventCountersListener" />.
/// </summary>
public class EventCountersCollectorOptions
{
    /// <summary>
    /// Gets or sets a list of EventSources and CounterNames to listen for.
    /// </summary>
    /// <value>
    /// The default value is an empty dictionary.
    /// </value>
    /// <remarks>
    /// It is a dictionary of EventSource to the set of counters that needs to be collected from the event source.
    /// See <see href="https://learn.microsoft.com/dotnet/core/diagnostics/available-counters" />
    /// for well known event counters and their availability.
    /// </remarks>
    [Required]
    public IDictionary<string, ISet<string>> Counters { get; set; }

    /// <summary>
    /// Gets or sets a sampling interval for counters.
    /// </summary>
    /// <value>
    /// The default value is 1 second.
    /// </value>
    [TimeSpan("00:00:01", "00:10:00")]
    public TimeSpan SamplingInterval { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include recommended default counters.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false" />. If <see cref="P:Microsoft.Extensions.Telemetry.Metering.EventCountersCollectorOptions.Counters" /> is empty, the default value is <see langword="true" /> so the
    /// default recommended counters are included when the listener is created.
    /// </value>
    /// <remarks>
    /// Includes the recommended default event counters in addition to the counters specified in <see cref="P:Microsoft.Extensions.Telemetry.Metering.EventCountersCollectorOptions.Counters" />.
    /// See the list of recommended default counters in <see href="https://eng.ms/docs/experiences-devices/r9-sdk/docs/telemetry/metering/event-counters" />.
    /// EventSource: "System.Runtime", Counters:
    ///   - "cpu-usage", "working-set", "time-in-gc", "alloc-rate", "exception-count", "gen-2-gc-count", "gen-2-size",
    ///   - "monitor-lock-contention-count", "active-timer-count", "threadpool-queue-length", "threadpool-thread-count",
    /// EventSource: "Microsoft-AspNetCore-Server-Kestrel", Counters:
    ///   - "connection-queue-length", "request-queue-length".
    /// </remarks>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public bool IncludeRecommendedDefault { get; set; }

    public EventCountersCollectorOptions();
}
