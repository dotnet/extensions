// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.EventSource;

namespace Microsoft.Extensions.Logging.Performance
{
    [AspNetCoreBenchmark]
    public class EventSourceBenchmark: LoggingBenchmarkBase
    {
        private ILogger _logger;

        private TestEventListener _listener;

        [Params(true, false)]
        public bool HasSubscribers { get; set; } = false;

        [Params(true, false)]
        public bool Json { get; set; } = false;

        [Benchmark]
        public void FilteredByLevel_InsideScope()
        {
            using (_logger.BeginScope("String scope"))
            {
                using (_logger.BeginScope(new SampleScope()))
                {
                    TwoArgumentErrorMessage(_logger, 1, "string", Exception);
                    TwoArgumentTraceMessage(_logger, 2, "string", Exception);
                }
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddEventSourceLogger());

            _logger = services.BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger("Logger");

            if (HasSubscribers)
            {
                _listener = new TestEventListener(Json ? LoggingEventSource.Keywords.JsonMessage : LoggingEventSource.Keywords.FormattedMessage);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _listener?.Dispose();
        }

        private class TestEventListener : EventListener
        {
            private readonly EventKeywords _keywords;

            public TestEventListener(EventKeywords keywords)
            {
                _keywords = keywords;
            }

            protected override void OnEventSourceCreated(System.Diagnostics.Tracing.EventSource eventSource)
            {
                if (eventSource.Name == "Microsoft-Extensions-Logging")
                {
                    EnableEvents(eventSource, EventLevel.Verbose, _keywords);
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventWrittenArgs)
            {
            }
        }
    }
}
