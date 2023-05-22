// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    /// <remarks>
    /// It is a dictionary of EventSource to the set of counters that needs to be collected from the event source.
    /// Please visit <see href="https://docs.microsoft.com/en-us/dotnet/core/diagnostics/available-counters"/>
    /// for well known event counters and their availability.
    /// Default set to an empty dictionary.
    /// </remarks>
    [Required]
    [Microsoft.Shared.Data.Validation.Length(1)]
#pragma warning disable CA2227 // Collection properties should be read only
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
    public IDictionary<string, ISet<string>> Counters { get; set; }
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning restore CA2227 // Collection properties should be read only
        = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a sampling interval for counters.
    /// </summary>
    /// <remarks>
    /// Default set to 1 second.
    /// </remarks>
    [TimeSpan("00:00:01", "00:10:00")]
    public TimeSpan SamplingInterval { get; set; } = _defaultSamplingInterval;

#if NET5_0_OR_GREATER
    /// <summary>
    /// Gets or sets the interval at which to recycle the <see cref="EventCountersListener"/>.
    /// </summary>
    /// <remarks>
    /// This is a work-around for this <see href="https://github.com/dotnet/runtime/issues/43985">issue</see>.
    /// Default set to 1 hour.
    /// This only has an effect on .NET 5, it is ignored for .NET 6 and above.
    /// </remarks>
    // The property is intended to be used on .NET 5 only, we ship it for newer TFMs to resolve package compositional issues. Refer to discussion for details:
    // https://domoreexp.visualstudio.com/R9/_git/SDK/pullrequest/552703?_a=files&path=/src/Extensions/Metering.Collectors.EventCounters/EventCountersCollectorOptions.cs&discussionId=9983912
    [TimeSpan("00:10:00", "06:00:00")]
    public TimeSpan EventListenerRecyclingInterval { get; set; } = _defaultEventListenerRecyclingInterval;
#endif
}
