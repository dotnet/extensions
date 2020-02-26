// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Logging.Performance
{
    [AspNetCoreBenchmark]
    public class FormattingBenchmarks : LoggingBenchmarkBase
    {
        private ILogger _logger;

        [Benchmark]
        public void TwoArguments()
        {
            TwoArgumentErrorMessage(_logger, 1, "string", Exception);
        }

        [Benchmark(Baseline = true)]
        public void NoArguments()
        {
            NoArgumentErrorMessage(_logger, Exception);
        }

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ILoggerProvider, LoggerProvider<MessageFormattingLogger>>();

            _logger = services.BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger("Logger");
        }

        public class MessageFormattingLogger: ILogger
        {
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                formatter(state, exception);
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }
        }
    }
}