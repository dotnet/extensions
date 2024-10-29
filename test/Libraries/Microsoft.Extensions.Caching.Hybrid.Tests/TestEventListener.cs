// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using Microsoft.Extensions.Caching.Hybrid.Internal;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public sealed class TestEventListener : EventListener
{
    // captures both event and counter data

    // this is used as a class fixture from HybridCacheEventSourceTests, because there
    // seems to be some unpredictable behaviours if multiple event sources/listeners are
    // casually created etc
    private const double EventCounterIntervalSec = 0.25;

    private readonly List<(int id, string name, EventLevel level)> _events = [];
    private readonly Dictionary<string, (string? displayName, double value)> _counters = [];

    private object SyncLock => _events;

    internal HybridCacheEventSource Source { get; } = new();

    public TestEventListener Reset(bool resetCounters = true)
    {
        lock (SyncLock)
        {
            _events.Clear();
            _counters.Clear();

            if (resetCounters)
            {
                Source.ResetCounters();
            }
        }

        Assert.True(Source.IsEnabled(), "should report as enabled");

        return this;
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (ReferenceEquals(eventSource, Source))
        {
            var args = new Dictionary<string, string?>
            {
                ["EventCounterIntervalSec"] = EventCounterIntervalSec.ToString("G", CultureInfo.InvariantCulture),
            };
            EnableEvents(Source, EventLevel.Verbose, EventKeywords.All, args);
        }

        base.OnEventSourceCreated(eventSource);
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (ReferenceEquals(eventData.EventSource, Source))
        {
            // capture counters/events
            lock (SyncLock)
            {
                if (eventData.EventName == "EventCounters"
                    && eventData.Payload is { Count: > 0 })
                {
                    foreach (var payload in eventData.Payload)
                    {
                        if (payload is IDictionary<string, object> map)
                        {
                            string? name = null;
                            string? displayName = null;
                            double? value = null;
                            bool isIncrement = false;
                            foreach (var pair in map)
                            {
                                switch (pair.Key)
                                {
                                    case "Name" when pair.Value is string:
                                        name = (string)pair.Value;
                                        break;
                                    case "DisplayName" when pair.Value is string s:
                                        displayName = s;
                                        break;
                                    case "Mean":
                                        isIncrement = false;
                                        value = Convert.ToDouble(pair.Value);
                                        break;
                                    case "Increment":
                                        isIncrement = true;
                                        value = Convert.ToDouble(pair.Value);
                                        break;
                                }
                            }

                            if (name is not null && value is not null)
                            {
                                if (isIncrement && _counters.TryGetValue(name, out var oldPair))
                                {
                                    value += oldPair.value; // treat as delta from old
                                }

                                Debug.WriteLine($"{name}={value}");
                                _counters[name] = (displayName, value.Value);
                            }
                        }
                    }
                }
                else
                {
                    _events.Add((eventData.EventId, eventData.EventName ?? "", eventData.Level));
                }
            }
        }

        base.OnEventWritten(eventData);
    }

    public (int id, string name, EventLevel level) SingleEvent()
    {
        (int id, string name, EventLevel level) evt;
        lock (SyncLock)
        {
            evt = Assert.Single(_events);
        }

        return evt;
    }

    public void AssertSingleEvent(int id, string name, EventLevel level)
    {
        var evt = SingleEvent();
        Assert.Equal(name, evt.name);
        Assert.Equal(id, evt.id);
        Assert.Equal(level, evt.level);
    }

    public double AssertCounter(string name, string displayName)
    {
        lock (SyncLock)
        {
            Assert.True(_counters.TryGetValue(name, out var pair), $"counter not found: {name}");
            Assert.Equal(displayName, pair.displayName);

            _counters.Remove(name); // count as validated
            return pair.value;
        }
    }

    public void AssertCounter(string name, string displayName, double expected)
    {
        var actual = AssertCounter(name, displayName);
        if (!Equals(expected, actual))
        {
            Assert.Fail($"{name}: expected {expected}, actual {actual}");
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality", Justification = "Test expects exact zero")]
    public void AssertRemainingCountersZero()
    {
        lock (SyncLock)
        {
            foreach (var pair in _counters)
            {
                if (pair.Value.value != 0)
                {
                    Assert.Fail($"{pair.Key}: expected 0, actual {pair.Value.value}");
                }
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Clarity and usability")]
    public Task TimeForCounters() => Task.Delay(TimeSpan.FromSeconds(EventCounterIntervalSec * 2));
}
