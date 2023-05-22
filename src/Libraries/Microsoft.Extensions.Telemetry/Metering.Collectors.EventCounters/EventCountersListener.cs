// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Metering.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Metering;

/// <summary>
/// An <see cref="EventListener"/> that allows to send data written by
/// instances of an <see cref="EventSource"/> to the metering pipeline.
/// </summary>
internal sealed class EventCountersListener : EventListener
{
    private readonly bool _isInitialized;
    private readonly Dictionary<string, string?> _eventSourceSettings;
    private readonly Meter _meter;
    private readonly FrozenDictionary<string, FrozenSet<string>> _counters;
    private readonly ILogger<EventCountersListener> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventCountersListener" /> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="meter">The meter provider.</param>
    /// <param name="logger">Logger instance.</param>
    public EventCountersListener(
        IOptions<EventCountersCollectorOptions> options,
        Meter<EventCountersListener> meter,
        ILogger<EventCountersListener>? logger = null)
    {
        var value = Throw.IfMemberNull(options, options.Value);
        _counters = CreateCountersDictionary(value.Counters);
        _meter = meter;
        _logger = logger ?? NullLoggerFactory.Instance.CreateLogger<EventCountersListener>();
        _eventSourceSettings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["EventCounterIntervalSec"] = value.SamplingInterval.TotalSeconds.ToString(CultureInfo.InvariantCulture)
        };

        _logger.ConfiguredEventCountersOptions(value);

        _isInitialized = true;
        foreach (var eventSource in EventSource.GetSources())
        {
            EnableIfNeeded(eventSource);
        }
    }

    /// <summary>
    /// Called for all existing event sources when the event listener is created and
    /// when a new event source is attached to the listener.
    /// </summary>
    /// <param name="eventSource">The event source.</param>
    /// <remarks>
    /// This method is called whenever a new eventSource is 'attached' to the dispatcher.
    /// This can happen for all existing EventSources when the EventListener is created
    /// as well as for any EventSources that come into existence after the EventListener has been created.
    /// These 'catch up' events are called during the construction of the EventListener.
    /// Subclasses need to be prepared for that. In a multi-threaded environment,
    /// it is possible that 'OnEventWritten' callbacks for a particular eventSource to occur
    /// BEFORE the OnEventSourceCreated is issued.
    /// </remarks>
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (!_isInitialized || eventSource == null)
        {
            return;
        }

        EnableIfNeeded(eventSource);
    }

    /// <summary>
    /// Called whenever an event has been written by an event source for which the event listener has enabled events.
    /// </summary>
    /// <param name="eventData">The event arguments that describe the event.</param>
    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (!_isInitialized)
        {
            return;
        }

        if (eventData.EventName == null)
        {
            _logger.EventNameIsNull(eventData.EventSource.Name);
            return;
        }

        if (eventData.Payload == null)
        {
            _logger.PayloadIsNull(eventData.EventSource.Name, eventData.EventName);
            return;
        }

        if (!eventData.EventName.Equals("EventCounters", StringComparison.OrdinalIgnoreCase))
        {
            _logger.EventNameIsNotEventCounters(eventData.EventName);
            return;
        }

        if (_counters.TryGetValue(eventData.EventSource.Name, out var counters))
        {
            for (var i = 0; i < eventData.Payload.Count; ++i)
            {
                if (eventData.Payload[i] is not IDictionary<string, object> eventPayload)
                {
                    continue;
                }

                if (!eventPayload.TryGetValue("CounterType", out var counterType))
                {
                    _logger.EventPayloadDoesNotContainCounterType(eventData.EventSource.Name, eventData.EventName);
                    continue;
                }

                if (!eventPayload.TryGetValue("Name", out var counterName))
                {
                    _logger.EventPayloadDoesNotContainName(eventData.EventSource.Name, eventData.EventName);
                    continue;
                }

                var name = counterName.ToString();

                if (string.IsNullOrEmpty(name))
                {
                    _logger.CounterNameIsEmpty(eventData.EventSource.Name, eventData.EventName);
                    continue;
                }

                if (!counters.Contains(name))
                {
                    _logger.CounterNotEnabled(name, eventData.EventSource.Name, eventData.EventName);
                    continue;
                }

                var type = counterType.ToString();

                _logger.ReceivedEventOfType(eventData.EventSource.Name, counterName, type);
                if ("Sum".Equals(type, StringComparison.OrdinalIgnoreCase))
                {
                    var fullName = $"{eventData.EventSource.Name}|{counterName}";
                    RecordSumEvent(eventPayload, fullName);
                }
                else if ("Mean".Equals(type, StringComparison.OrdinalIgnoreCase))
                {
                    var fullName = $"{eventData.EventSource.Name}|{counterName}";
                    RecordMeanEvent(eventPayload, fullName);
                }
            }
        }
    }

    private static FrozenDictionary<string, FrozenSet<string>> CreateCountersDictionary(IDictionary<string, ISet<string>> counters)
    {
        var d = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in counters)
        {
            if (kvp.Value == null || kvp.Value.Count == 0)
            {
                continue;
            }

            d.Add(kvp.Key, new HashSet<string>(kvp.Value, StringComparer.OrdinalIgnoreCase));
        }

        return d.Select(x => new KeyValuePair<string, FrozenSet<string>>(x.Key,
            x.Value.ToFrozenSet(StringComparer.Ordinal, optimizeForReading: true))).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase, optimizeForReading: true);
    }

    private bool TryConvertToLong(object? value, out long convertedValue)
    {
        try
        {
            convertedValue = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch (OverflowException)
        {
            // The number is invalid, ignore processing it and don't bubble up exception.
            _logger.OverflowExceptionWhileConversion(value);
        }
        catch (FormatException)
        {
            // The number is invalid, ignore processing it and don't bubble up exception.
            _logger.FormatExceptionWhileConversion(value);
        }

        convertedValue = 0;
        return false;
    }

    private void EnableIfNeeded(EventSource eventSource)
    {
        if (!_counters.ContainsKey(eventSource.Name))
        {
            return;
        }

        _logger.EnablingEventSource(eventSource.Name);
        EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All, _eventSourceSettings);
    }

    private void RecordSumEvent(IDictionary<string, object> eventPayload, string counterName)
    {
        if (!eventPayload.TryGetValue("Increment", out var payloadValue))
        {
            return;
        }

        if (TryConvertToLong(payloadValue, out long metricValue))
        {
            var metric = _meter.CreateCounter<long>(counterName);
            metric.Add(metricValue);
        }
    }

    private void RecordMeanEvent(IDictionary<string, object> eventPayload, string counterName)
    {
        if (!eventPayload.TryGetValue("Mean", out var payloadValue))
        {
            return;
        }

        if (TryConvertToLong(payloadValue, out long metricValue))
        {
            var metric = _meter.CreateHistogram<long>(counterName);
            metric.Record(metricValue);
        }
    }
}
