// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Telemetry.Metering;

/// <summary>
/// Configuration options for <see cref="EventCountersListener"/>.
/// </summary>
public class EventCountersCollectorOptions
{
    private static readonly TimeSpan _defaultSamplingInterval = TimeSpan.FromSeconds(1);

#if NET5_0_OR_GREATER
    /// <remarks>
    /// This is a work-around for this <see href="https://github.com/dotnet/runtime/issues/43985">issue</see>.
    /// The field is intended to be used on .NET 5 only, we ship it for newer TFMs to resolve package compositional issues.
    /// See discussion in <see href="https://domoreexp.visualstudio.com/R9/_git/SDK/pullrequest/552703"/> for additional context.
    /// </remarks>
    private static readonly TimeSpan _defaultEventListenerRecyclingInterval = TimeSpan.FromHours(1);
#endif

    /// <summary>
    /// Gets or sets a list of EventSources and CounterNames to listen for.
    /// </summary>
    /// <value>
    /// The default value is an empty dictionary.
    /// </value>
    /// <remarks>
    /// It is a dictionary of EventSource to the set of counters that needs to be collected from the event source.
    /// See <see href="https://learn.microsoft.com/dotnet/core/diagnostics/available-counters"/>
    /// for well known event counters and their availability.
    /// </remarks>
    [Required]
#pragma warning disable CA2227 // Collection properties should be read only
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
    public IDictionary<string, ISet<string>> Counters { get; set; }
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning restore CA2227 // Collection properties should be read only
        = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a sampling interval for counters.
    /// </summary>
    /// <value>
    /// The default value is 1 second.
    /// </value>
    [TimeSpan("00:00:01", "00:10:00")]
    public TimeSpan SamplingInterval { get; set; } = _defaultSamplingInterval;

    /// <summary>
    /// Gets or sets a value indicating whether to include recommended default counters.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false" />. If <see cref="Counters"/> is empty, the default value is <see langword="true" /> so the
    /// default recommended counters are included when the listener is created.
    /// </value>
    /// <remarks>
    /// Includes the recommended default event counters in addition to the counters specified in <see cref="Counters"/>.
    /// See the list of recommended default counters in <see href="https://eng.ms/docs/experiences-devices/r9-sdk/docs/telemetry/metering/event-counters"/>.
    /// EventSource: "System.Runtime", Counters:
    ///   - "cpu-usage", "working-set", "time-in-gc", "alloc-rate", "exception-count", "gen-2-gc-count", "gen-2-size",
    ///   - "monitor-lock-contention-count", "active-timer-count", "threadpool-queue-length", "threadpool-thread-count",
    /// EventSource: "Microsoft-AspNetCore-Server-Kestrel", Counters:
    ///   - "connection-queue-length", "request-queue-length".
    /// </remarks>
    [Experimental]
    public bool IncludeRecommendedDefault { get; set; }

#if NET5_0_OR_GREATER
    /// <summary>
    /// Gets or sets the interval at which to recycle the <see cref="EventCountersListener"/>.
    /// </summary>
    /// <value>
    /// The default value is 1 hour.
    /// </value>
    /// <remarks>
    /// This property is a work-around for <see href="https://github.com/dotnet/runtime/issues/43985">dotnet/runtime issue 43985</see>.
    /// It only has an effect on .NET 5, and is ignored for .NET 6 and later versions.
    /// </remarks>
    // The property is intended to be used on .NET 5 only, we ship it for newer TFMs to resolve package compositional issues. Refer to discussion for details:
    // https://domoreexp.visualstudio.com/R9/_git/SDK/pullrequest/552703?_a=files&path=/src/Extensions/Metering.Collectors.EventCounters/EventCountersCollectorOptions.cs&discussionId=9983912
    [TimeSpan("00:10:00", "06:00:00")]
    public TimeSpan EventListenerRecyclingInterval { get; set; } = _defaultEventListenerRecyclingInterval;
#endif
}
