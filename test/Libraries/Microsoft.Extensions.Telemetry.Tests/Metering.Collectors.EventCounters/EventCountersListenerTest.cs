// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
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

    private sealed class InstrumentCounter : IDisposable
    {
        private readonly MeterListener _meterListener = new();

        public InstrumentCounter(Meter meter)
        {
            _meterListener = new MeterListener
            {
                InstrumentPublished = (instrument, _) =>
                {
                    if (instrument.Meter == meter)
                    {
                        Count++;
                    }
                }
            };

            _meterListener.Start();
        }

        public void Dispose() => _meterListener.Dispose();
        public int Count { get; private set; }
    }

    [Fact]
    public void EventCountersListener_Ignores_NonStructuredEvents()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

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

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Ignores_UnknownCounterTypes()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = _fixture.Create<string>(), Name = _counterName, Increment = 1 }
            });

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_ValidateEventCounterInterval_SetCorrectly()
    {
        using var meter = new Meter<EventCountersListener>();

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
        using var instrumentCounter = new InstrumentCounter(meter);

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

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_OnEventWritten_Ignores_WithEmptyCounterName()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);

        string emptyCounterName = string.Empty;
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = emptyCounterName, Increment = 1 }
            });

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_OnEventWritten_Ignores_WithoutEventName()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        using var eventSource = new TestEventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);

        eventSource.Write(null,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = 1 }
            });

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_OnEventWritten_Ignores_WithoutPayload()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        using var eventSource = new TestEventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);

        eventSource.Write(_eventSourceName,
            new EventSourceOptions { Level = EventLevel.LogAlways });

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_OnEventWritten_Ignores_WithoutEventData()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        using var eventSource = new TestEventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);

        eventSource.Write(_eventSourceName);

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Ignores_UnknownEventSources()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        SendMeanEvent(meter,
            MeanEventProperties.All,
            eventSourceName: _fixture.Create<string>(),
            counterName: _counterName,
            eventName: EventName);

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Ignores_UnknownEvents()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        SendMeanEvent(meter,
            MeanEventProperties.All,
            eventSourceName: _eventSourceName,
            counterName: _counterName,
            eventName: _fixture.Create<string>());

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Ignores_UnknownCounters()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        _options.Value.Counters.Clear();

        SendMeanEvent(meter, MeanEventProperties.All);

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Ignores_EventsWithoutCounterType()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        SendMeanEvent(meter, MeanEventProperties.All ^ MeanEventProperties.WithType);

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Ignores_EventsWithoutName()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        SendMeanEvent(meter, MeanEventProperties.All ^ MeanEventProperties.WithName);

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Ignores_MeanEventsWithoutMean()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        SendMeanEvent(meter, MeanEventProperties.All ^ MeanEventProperties.WithMean);

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Ignores_Empty_Counters_Maps()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);
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

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Emits_WhenSumCounterMatches()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector<long>(meter, $"{_eventSourceName}|{_counterName}");

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = 1 }
            });

        var latest = metricCollector.LastMeasurement;
        Assert.NotNull(latest);
        Assert.Equal(1, latest.Value);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenSumCounterMatches_ValueIsDoublePositiveInfinity()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = double.PositiveInfinity }
            });

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenSumCounterMatches_ValueIsDoubleNegativeInfinity()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = double.NegativeInfinity }
            });

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenSumCounterMatches_ValueIsDoubleNan()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = double.NaN }
            });

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenSumCounterMatches_ValueIncorrectlyFormatted()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = "?/str." }
            });

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Emits_WhenSourceIsCreatedAfterListener()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector<long>(meter, $"{_eventSourceName}|{_counterName}");

        using var listener = new EventCountersListener(_options, meter);
        using var eventSource = new EventSource(_eventSourceName);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = 1 }
            });

        var latest = metricCollector.LastMeasurement;
        Assert.NotNull(latest);
        Assert.Equal(1, latest.Value);
    }

    [Fact]
    public void EventCountersListener_Ignores_SumCounterWithoutIncrement()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);
        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName }
            });

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Emits_WhenMeanCounterMatches()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector<long>(meter, $"{_eventSourceName}|{_counterName}");

        SendMeanEvent(meter, MeanEventProperties.All);

        var latest = metricCollector.LastMeasurement;
        Assert.NotNull(latest);
        Assert.Equal(1, latest.Value);
    }

    [Fact]
    public void EventCountersListener_MultipleEventsForSameMetricShouldUseCachedHistogram()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector<long>(meter, $"{_eventSourceName}|{_counterName}");

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);

        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Mean", Name = _counterName, Mean = 1 }
            });

        var latest = metricCollector.LastMeasurement;
        Assert.NotNull(latest);
        Assert.Equal(1, latest.Value);
        Assert.Single(listener.HistogramInstruments);

        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Mean", Name = _counterName, Mean = 1 }
            });

        latest = metricCollector.LastMeasurement;
        Assert.NotNull(latest);
        Assert.Single(listener.HistogramInstruments);
    }

    [Fact]
    public void EventCountersListener_MultipleEventsForSameMetricShouldUseCachedCounter()
    {
        using var meter = new Meter<EventCountersListener>();
        using var metricCollector = new MetricCollector<long>(meter, $"{_eventSourceName}|{_counterName}");

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(_options, meter);

        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = 1 }
            });

        var latest = metricCollector.LastMeasurement;
        Assert.NotNull(latest);
        Assert.Equal(1, latest.Value);
        Assert.Single(listener.CounterInstruments);

        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = 1 }
            });

        latest = metricCollector.LastMeasurement;
        Assert.NotNull(latest);
        Assert.Single(listener.CounterInstruments);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenMeanCounterMatches_MeanValueDoubleNan()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);
        SendMeanEvent(meter, MeanEventProperties.All, _eventSourceName, _counterName, EventName, meanValue: double.NaN);

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenMeanCounterMatches_MeanValuePositiveInfinity()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);
        SendMeanEvent(meter, MeanEventProperties.All, _eventSourceName, _counterName, EventName, meanValue: double.PositiveInfinity);

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenMeanCounterMatches_MeanValueNegativeInfinity()
    {
        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);
        SendMeanEvent(meter, MeanEventProperties.All, _eventSourceName, _counterName, EventName, meanValue: double.NegativeInfinity);

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_Ignores_WhenCounterNameIsNotRegistered()
    {
        var options = Create(new EventCountersCollectorOptions());
        options.Value.Counters.Add(_eventSourceName, new HashSet<string> { _counterName });

        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(options, meter);

        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = "randomCounterName", Increment = 1 }
            });

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_IncludeRecommendedDefault_AddsDefaultCountersToCounterList()
    {
        using var meter = new Meter<EventCountersListener>();
        using var listener = new EventCountersListener(Create(new EventCountersCollectorOptions { IncludeRecommendedDefault = true }), meter);

        Assert.NotEmpty(listener.Counters);

        foreach (var counterSet in listener.Counters)
        {
            if (counterSet.Key == "System.Runtime")
            {
                Assert.Equal(11, counterSet.Value.Count);
                Assert.Contains("cpu-usage", counterSet.Value);
                Assert.Contains("working-set", counterSet.Value);
                Assert.Contains("time-in-gc", counterSet.Value);
                Assert.Contains("alloc-rate", counterSet.Value);
                Assert.Contains("exception-count", counterSet.Value);
                Assert.Contains("gen-2-gc-count", counterSet.Value);
                Assert.Contains("gen-2-size", counterSet.Value);
                Assert.Contains("monitor-lock-contention-count", counterSet.Value);
                Assert.Contains("active-timer-count", counterSet.Value);
                Assert.Contains("threadpool-queue-length", counterSet.Value);
                Assert.Contains("threadpool-thread-count", counterSet.Value);
            }
            else if (counterSet.Key == "Microsoft-AspNetCore-Server-Kestrel")
            {
                Assert.Equal(2, counterSet.Value.Count);
                Assert.Contains("connection-queue-length", counterSet.Value);
                Assert.Contains("request-queue-length", counterSet.Value);
            }
            else
            {
                Assert.Fail($"Unexpected counter set {counterSet.Key}");
            }
        }
    }

    [Fact]
    public void EventCountersListener_DuplicateEntriesAreIgnored()
    {
        using var meter = new Meter<EventCountersListener>();
        IDictionary<string, ISet<string>> counters = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { _eventSourceName, new HashSet<string> { _counterName } },
                { TestUtils.SystemRuntime, new HashSet<string> { "active-timer-count", "cpu-usage" } }
            };
        using var listener = new EventCountersListener(Create(new EventCountersCollectorOptions { IncludeRecommendedDefault = true, Counters = counters }), meter);

        Assert.NotEmpty(listener.Counters);

        foreach (var counterSet in listener.Counters)
        {
            if (counterSet.Key == "System.Runtime")
            {
                Assert.Equal(11, counterSet.Value.Count);
                Assert.Contains("cpu-usage", counterSet.Value);
                Assert.Contains("working-set", counterSet.Value);
                Assert.Contains("time-in-gc", counterSet.Value);
                Assert.Contains("alloc-rate", counterSet.Value);
                Assert.Contains("exception-count", counterSet.Value);
                Assert.Contains("gen-2-gc-count", counterSet.Value);
                Assert.Contains("gen-2-size", counterSet.Value);
                Assert.Contains("monitor-lock-contention-count", counterSet.Value);
                Assert.Contains("active-timer-count", counterSet.Value);
                Assert.Contains("threadpool-queue-length", counterSet.Value);
                Assert.Contains("threadpool-thread-count", counterSet.Value);
            }
            else if (counterSet.Key == "Microsoft-AspNetCore-Server-Kestrel")
            {
                Assert.Equal(2, counterSet.Value.Count);
                Assert.Contains("connection-queue-length", counterSet.Value);
                Assert.Contains("request-queue-length", counterSet.Value);
            }
            else if (counterSet.Key == _eventSourceName)
            {
                Assert.Single(counterSet.Value);
                Assert.Contains(_counterName, counterSet.Value);
            }
            else
            {
                Assert.Fail($"Unexpected counter set {counterSet.Key}");
            }
        }
    }

    [Fact]
    public void EventCountersListener_UsingWildcard_EnablesAllCountersForSource()
    {
        var firstCounterName = "randomCounterName";
        var secondCounterName = "randomCounterName2";

        using var meter = new Meter<EventCountersListener>();
        using var metricCollector1 = new MetricCollector<long>(meter, $"{_eventSourceName}|{firstCounterName}");
        using var metricCollector2 = new MetricCollector<long>(meter, $"{_eventSourceName}|{secondCounterName}");
        IDictionary<string, ISet<string>> counters = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { _eventSourceName, new HashSet<string> { "*" } }
            };

        using var eventSource = new EventSource(_eventSourceName);
        using var listener = new EventCountersListener(Create(new EventCountersCollectorOptions { Counters = counters }), meter);

        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Mean", Name = firstCounterName, Mean = 1 }
            });

        var latest = metricCollector1.LastMeasurement;
        Assert.NotNull(latest);
        Assert.Equal(1, latest.Value);
        Assert.Single(listener.HistogramInstruments);

        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Mean", Name = secondCounterName, Mean = 1 }
            });

        latest = metricCollector2.LastMeasurement;
        Assert.NotNull(latest);
        Assert.Equal(1, latest.Value);
        Assert.Equal(2, listener.HistogramInstruments.Count);
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
        using var metricCollector = new MetricCollector<long>(meter, metricName);
        RunListener(options, meter, eventWaitHandle);

        var latest = metricCollector.LastMeasurement;
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
        using var metricCollector = new MetricCollector<long>(meter, metricName);
        RunListener(options, meter, eventWaitHandle);

        var latest = metricCollector.LastMeasurement;
        Assert.NotNull(latest);
        Assert.True(latest.Value >= 0);
    }

    [Fact]
    public void EventCountersListener_IncludeRecommendedDefault_AddsDefaultCounters()
    {
        string counterName = "alloc-rate";
        string metricName = $"{TestUtils.SystemRuntime}|{counterName}";

        using var meter = new Meter<EventCountersListener>();
        using var metricCollector1 = new MetricCollector<long>(meter, metricName);
        using var metricCollector2 = new MetricCollector<long>(meter, $"{_eventSourceName}|{_counterName}");

        IDictionary<string, ISet<string>> counters = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { _eventSourceName, new HashSet<string> { _counterName } },
            };

        IOptions<EventCountersCollectorOptions> options = Create(new EventCountersCollectorOptions
        {
            IncludeRecommendedDefault = true,
            Counters = counters,
            SamplingInterval = TimeSpan.FromMilliseconds(10)
        });

        using var eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        RunListener(options, meter, eventWaitHandle);

        var latest = metricCollector1.LastMeasurement;
        Assert.NotNull(latest);
        Assert.True(latest.Value >= 0);

        using var eventSource = new TestEventSource(_eventSourceName);
        using var listener = new EventCountersListener(options, meter);

        eventSource.Write(EventName,
            new EventSourceOptions { Level = EventLevel.LogAlways },
            new
            {
                payload = new { CounterType = "Sum", Name = _counterName, Increment = 1 }
            });

        latest = metricCollector2.LastMeasurement;
        Assert.NotNull(latest);
        Assert.Equal(1, latest.Value);
    }

    [Fact]
    public void EventCountersListener_IncludeRecommendedDefault_AndDuplicateEntryInProvidedCounters()
    {
        string counterName = "alloc-rate";
        string metricName = $"{TestUtils.SystemRuntime}|{counterName}";

        using var meter = new Meter<EventCountersListener>();
        using var metricCollector1 = new MetricCollector<long>(meter, metricName);
        using var metricCollector2 = new MetricCollector<long>(meter, $"{TestUtils.SystemRuntime}|active-timer-count");

        IDictionary<string, ISet<string>> counters = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { TestUtils.SystemRuntime, new HashSet<string> { "active-timer-count" } }
            };

        IOptions<EventCountersCollectorOptions> options = Create(new EventCountersCollectorOptions
        {
            IncludeRecommendedDefault = true,
            Counters = counters,
            SamplingInterval = TimeSpan.FromMilliseconds(10)
        });

        using var eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        RunListener(options, meter, eventWaitHandle);

        var latest = metricCollector1.LastMeasurement;
        Assert.NotNull(latest);
        Assert.True(latest.Value >= 0);

        latest = metricCollector2.LastMeasurement;
        Assert.NotNull(latest);
        Assert.True(latest.Value >= 0);
    }

    [Fact]
    public void EventCountersListener_IncludeRecommendedDefault_SetToFalse_DoesNot_IncludesDefaultCounters()
    {
        string counterName = "alloc-rate";
        string metricName = $"{TestUtils.SystemRuntime}|{counterName}";

        using var meter = new Meter<EventCountersListener>();
        using var instrumentCounter = new InstrumentCounter(meter);

        IOptions<EventCountersCollectorOptions> options = Create(new EventCountersCollectorOptions());

        using var eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        RunListener(options, meter, eventWaitHandle);

        Assert.Equal(0, instrumentCounter.Count);
    }

    [Fact]
    public void EventCountersListener_UsingWildcard_IncludesAllCountersFromSource()
    {
        string counterName = "alloc-rate";
        string metricName = $"{TestUtils.SystemRuntime}|{counterName}";

        using var meter = new Meter<EventCountersListener>();
        using var metricCollector1 = new MetricCollector<long>(meter, metricName);
        using var metricCollector2 = new MetricCollector<long>(meter, $"{TestUtils.SystemRuntime}|active-timer-count");

        IDictionary<string, ISet<string>> counters = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { TestUtils.SystemRuntime, new HashSet<string> { "*" } }
            };

        IOptions<EventCountersCollectorOptions> options = Create(new EventCountersCollectorOptions
        {
            Counters = counters,
            SamplingInterval = TimeSpan.FromMilliseconds(10)
        });

        using var eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        RunListener(options, meter, eventWaitHandle);

        var latest = metricCollector1.LastMeasurement;
        Assert.NotNull(latest);
        Assert.True(latest.Value >= 0);

        latest = metricCollector2.LastMeasurement;
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
