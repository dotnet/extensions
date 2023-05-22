// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
#if !NETFRAMEWORK
using System.Threading;
#endif
using AutoFixture;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Extensions.Telemetry.Metering.Test.Auxiliary;
using Microsoft.Extensions.Telemetry.Testing.Metering;
using Xunit;

using static Microsoft.Extensions.Options.Options;

namespace Microsoft.Extensions.Telemetry.Metering.Test;

public class EventCountersListenerTest
{
    private const string EventName = "EventCounters";
    private readonly Fixture _fixture = new();
    private readonly IOptions<EventCountersCollectorOptions> _options;
    private readonly string _eventSourceName;
    private readonly string _counterName;
    private readonly ILogger<EventCountersListener> _logger;

    public EventCountersListenerTest()
    {
        _options = Create(new EventCountersCollectorOptions());
        _eventSourceName = _fixture.Create<string>();
        _counterName = _fixture.Create<string>();
        _options.Value.Counters.Add(_eventSourceName, new HashSet<string> { _counterName });
        _logger = NullLoggerFactory.Instance.CreateLogger<EventCountersListener>();
    }

    [Fact]
    public void EventCountersListener_WhenNullOptions_Throws()
    {
        using var meter = new Meter<EventCountersListener>();
        Assert.Throws<ArgumentException>(() => new EventCountersListener(Create<EventCountersCollectorOptions>(null!), meter));
    }

    [Fact]
    public void EventCountersListener_Ignores_NonStructuredEvents()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter, _logger);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                CounterType = "Sum",
                Name = _counterName,
                Increment = 1
            });

        var counters = metricCollector.GetAllCounters<long>();
        var histograms = metricCollector.GetAllCounters<long>();

        Assert.Empty(counters!);
        Assert.Empty(histograms!);
    }

    [Fact]
    public void EventCountersListener_Ignores_UnknownCounterTypes()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = _fixture.Create<string>(), Name = _counterName, Increment = 1 }
            });

        var counters = metricCollector.GetAllCounters<long>();
        var histograms = metricCollector.GetAllCounters<long>();

        Assert.Empty(counters!);
        Assert.Empty(histograms!);
    }

    [Fact]
    public void EventCountersListener_ValidateEventCounterInterval_SetCorrectly()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        var intervalInSeconds = 2;

        IOptions<EventCountersCollectorOptions> options = Create(new EventCountersCollectorOptions
        {
            SamplingInterval = TimeSpan.FromSeconds(intervalInSeconds),
        });

        options.Value.Counters.Add(_eventSourceName, new HashSet<string> { _counterName });

        using var eventSource = new TestEventSource(_eventSourceName);
        using var listener = new EventCountersListener(options, meter);

        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = 1 }
            });

        Assert.True(eventSource.EventCommandEventArgs?.Arguments?.ContainsKey("EventCounterIntervalSec"));
        Assert.Equal($"{intervalInSeconds}", eventSource.EventCommandEventArgs?.Arguments?["EventCounterIntervalSec"]);
    }

    [Fact]
    public void EventCountersListener_OnEventWritten_Ignores_WithNullCounter()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        IDictionary<string, ISet<string>> counters = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { _eventSourceName, null! }
            };

        IOptions<EventCountersCollectorOptions> options = Create(new EventCountersCollectorOptions
        {
            Counters = counters
        });

        using var eventSource = new TestEventSource(_eventSourceName);
        using var listener = new EventCountersListener(options, meter);

        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = 1 }
            });

        Assert.Empty(metricCollector.GetAllHistograms<long>()!);
        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_OnEventWritten_Ignores_WithEmptyCounterName()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);

        string emptyCounterName = string.Empty;
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = emptyCounterName, Increment = 1 }
            });

        Assert.Empty(metricCollector.GetAllHistograms<long>()!);
        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_OnEventWritten_Ignores_WithoutEventName()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        using var eventSource = new TestEventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);

        eventSource.Write(null,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = 1 }
            });

        Assert.Empty(metricCollector.GetAllHistograms<long>()!);
        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_OnEventWritten_Ignores_WithoutPayload()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        using var eventSource = new TestEventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);

        eventSource.Write(_eventSourceName,
            new EventSourceOptions { Level = EventLevel.LogAlways });

        Assert.Empty(metricCollector.GetAllHistograms<long>()!);
        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_OnEventWritten_Ignores_WithoutEventData()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        using var eventSource = new TestEventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);

        eventSource.Write(_eventSourceName);

        Assert.Empty(metricCollector.GetAllHistograms<long>()!);
        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_Ignores_UnknownEventSources()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        SendMeanEvent(meter,
            MeanEventProperties.All,
            eventSourceName: _fixture.Create<string>(),
            counterName: _counterName,
            eventName: EventName);

        Assert.Empty(metricCollector.GetAllHistograms<long>()!);
        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_Ignores_UnknownEvents()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        SendMeanEvent(meter,
            MeanEventProperties.All,
            eventSourceName: _eventSourceName,
            counterName: _counterName,
            eventName: _fixture.Create<string>());

        Assert.Empty(metricCollector.GetAllHistograms<long>()!);
        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_Ignores_UnknownCounters()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);
        _options.Value.Counters.Clear();

        SendMeanEvent(meter, MeanEventProperties.All);

        Assert.Empty(metricCollector.GetAllHistograms<long>()!);
        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_Ignores_EventsWithoutCounterType()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        SendMeanEvent(meter, MeanEventProperties.All ^ MeanEventProperties.WithType);

        Assert.Empty(metricCollector.GetAllHistograms<long>()!);
        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_Ignores_EventsWithoutName()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        SendMeanEvent(meter, MeanEventProperties.All ^ MeanEventProperties.WithName);

        Assert.Empty(metricCollector.GetAllHistograms<long>()!);
        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_Ignores_MeanEventsWithoutMean()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        SendMeanEvent(meter, MeanEventProperties.All ^ MeanEventProperties.WithMean);

        Assert.Empty(metricCollector.GetAllHistograms<long>()!);
        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_Ignores_Empty_Counters_Maps()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);
        var eventSourceName = Guid.NewGuid().ToString();
        var options = new EventCountersCollectorOptions();
        options.Counters.Add(eventSourceName, new HashSet<string>());
        using var eventSource = new EventSource(eventSourceName);
        using var listener = new EventCountersListener(_options, meter);

        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = 1 }
            });

        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_Emits_WhenSumCounterMatches()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = 1 }
            });

        var latest = metricCollector.GetCounterValues<long>($"{_eventSourceName}|{_counterName}")!.LatestWritten!;
        Assert.NotNull(latest);
        Assert.Equal(1, latest.Value);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenSumCounterMatches_ValueIsDoublePositiveInfinity()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = double.PositiveInfinity }
            });

        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenSumCounterMatches_ValueIsDoubleNegativeInfinity()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = double.NegativeInfinity }
            });

        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenSumCounterMatches_ValueIsDoubleNan()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = double.NaN }
            });

        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenSumCounterMatches_ValueIncorrectlyFormatted()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = "?/str." }
            });

        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_Emits_WhenSourceIsCreatedAfterListener()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        using var listener = new EventCountersListener(_options, meter);
        using var eventSource = new EventSource(_eventSourceName);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = 1 }
            });

        var latest = metricCollector.GetCounterValues<long>($"{_eventSourceName}|{_counterName}")!.LatestWritten!;
        Assert.NotNull(latest);
        Assert.Equal(1, latest.Value);
    }

    [Fact]
    public void EventCountersListener_Ignores_SumCounterWithoutIncrement()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName }
            });

        Assert.Empty(metricCollector.GetAllCounters<long>()!);
    }

    [Fact]
    public void EventCountersListener_Emits_WhenMeanCounterMatches()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        SendMeanEvent(meter, MeanEventProperties.All);

        var latest = metricCollector.GetHistogramValues<long>($"{_eventSourceName}|{_counterName}")!.LatestWritten!;
        Assert.NotNull(latest);
        Assert.Equal(1, latest.Value);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenMeanCounterMatches_MeanValueDoubleNan()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);
        SendMeanEvent(meter, MeanEventProperties.All, _eventSourceName, _counterName, EventName, meanValue: double.NaN);

        Assert.Empty(metricCollector.GetAllHistograms<long>()!);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenMeanCounterMatches_MeanValuePositiveInfinity()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);
        SendMeanEvent(meter, MeanEventProperties.All, _eventSourceName, _counterName, EventName, meanValue: double.PositiveInfinity);

        Assert.Empty(metricCollector.GetAllHistograms<long>()!);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenMeanCounterMatches_MeanValueNegativeInfinity()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);
        SendMeanEvent(meter, MeanEventProperties.All, _eventSourceName, _counterName, EventName, meanValue: double.NegativeInfinity);

        Assert.Empty(metricCollector.GetAllHistograms<long>()!);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenCounterNameIsNotRegistered()
    {
        var options = Create(new EventCountersCollectorOptions());
        options.Value.Counters.Add(_eventSourceName, new HashSet<string> { _counterName });

        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(options, meter);

        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = "randomCounterName", Increment = 1 }
            });

        Assert.Empty(metricCollector.GetAllCounters<long>()!);
        Assert.Empty(metricCollector.GetAllHistograms<long>()!);
    }

    [Flags]
    private enum MeanEventProperties
    {
        None = 0,
        WithName = 1,
        WithType = 2,
        WithMean = 4,
        All = WithMean | WithType
    }

    private void SendMeanEvent(Meter<EventCountersListener> meter, MeanEventProperties meanEventProperties)
    {
        SendMeanEvent(meter, meanEventProperties, _eventSourceName, _counterName, EventName);
    }

    private void SendMeanEvent(Meter<EventCountersListener> meter,
        MeanEventProperties meanEventProperties,
        string eventSourceName,
        string counterName,
        string eventName,
        double meanValue = 1)
    {
        using var eventSource = new EventSource(eventSourceName);
        using var listener = new EventCountersListener(_options, meter);

        switch (meanEventProperties)
        {
            case MeanEventProperties.All:
                eventSource.Write(eventName,
                    new EventSourceOptions { Level = EventLevel.LogAlways },
                    new
                    {
                        payload = new { CounterType = "Mean", Name = counterName, Mean = meanValue }
                    });
                break;

            case MeanEventProperties.All ^ MeanEventProperties.WithMean:
                eventSource.Write(eventName,
                    new EventSourceOptions { Level = EventLevel.LogAlways },
                    new
                    {
                        payload = new { CounterType = "Mean", Name = counterName }
                    });
                break;

            case MeanEventProperties.All ^ MeanEventProperties.WithName:
                eventSource.Write(eventName,
                    new EventSourceOptions { Level = EventLevel.LogAlways },
                    new
                    {
                        payload = new { CounterType = "Mean", Mean = meanValue }
                    });
                break;

            case MeanEventProperties.All ^ MeanEventProperties.WithType:
                eventSource.Write(eventName,
                    new EventSourceOptions { Level = EventLevel.LogAlways },
                    new
                    {
                        payload = new { Name = counterName, Mean = meanValue }
                    });
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(meanEventProperties), meanEventProperties, "unsupported combination");
        }
    }

#if !NETFRAMEWORK
    [Fact]
    public void MeanCounter()
    {
        string counterName = "active-timer-count";
        string metricName = $"{TestUtils.SystemRuntime}|{counterName}";
        var options = TestUtils.CreateOptions(TestUtils.SystemRuntime, counterName);

        using var eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);
        RunListener(options, meter, eventWaitHandle);

        var latest = metricCollector.GetHistogramValues<long>(metricName)!.LatestWritten!;
        Assert.NotNull(latest);
        Assert.True(latest.Value >= 0);
    }

    [Fact]
    public void SumCounter()
    {
        string counterName = "alloc-rate";
        string metricName = $"{TestUtils.SystemRuntime}|{counterName}";
        var options = TestUtils.CreateOptions(TestUtils.SystemRuntime, counterName);

        using var eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector(meter);
        RunListener(options, meter, eventWaitHandle);

        var latest = metricCollector.GetCounterValues<long>(metricName)!.LatestWritten!;
        Assert.NotNull(latest);
        Assert.True(latest.Value >= 0);
    }

    private static void RunListener(IOptions<EventCountersCollectorOptions> options, Meter<EventCountersListener> meter, WaitHandle eventWaitHandle)
    {
        using var eventListener = new EventCountersListener(options, meter);
        eventWaitHandle.WaitOne(TimeSpan.FromMilliseconds(1000));
    }
#endif
}
