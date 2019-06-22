// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Logging.Performance
{
    [AspNetCoreBenchmark]
    public class FormattingBenchmarks : LoggingBenchmarkBase
    {
        [ParamsSource(nameof(Loggers))]
        public ILogger Logger;

        [Benchmark]
        public void TwoArguments()
        {
            TwoArgumentErrorMessage(Logger, 1, "string", Exception);
        }

        [Benchmark(Baseline = true)]
        public void NoArguments()
        {
            NoArgumentErrorMessage(Logger, Exception);
        }

        public IEnumerable<ILogger> Loggers => new[]
        {
            CreateLogger<MessageFormattingLogger>(),
            CreateLogger<MessageDirectLogger>()
        };

        static ILogger CreateLogger<TLogger>()
            where TLogger : ILogger, new()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ILoggerProvider, LoggerProvider<TLogger>>();
            return services.BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger("Logger");
        }

        public class MessageFormattingLogger : ILogger
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

            public override string ToString()
            {
                return "Formatting logger";
            }
        }

        class MessageDirectLogger : ILogger
        {
            readonly IExternalLogger logger;

            public MessageDirectLogger()
            {
                logger = new NoopExternalLogger();
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (state is ILogValues logValues)
                {
                    if (exception == null)
                    {
                        var wrapper = new Wrapper(logger, logLevel, eventId);
                        logValues.Log(ref wrapper);
                    }
                    else
                    {
                        formatter(state, exception);
                    }
                }
                else
                {
                    formatter(state, exception);
                }
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }

            public override string ToString()
            {
                return "Formatting logger";
            }

            readonly struct Wrapper : ITypedLogger
            {
                readonly IExternalLogger logger;
                readonly LogLevel logValues;
                readonly EventId eventId;

                public Wrapper(IExternalLogger logger, LogLevel logValues, in EventId eventId)
                {
                    this.logger = logger;
                    this.logValues = logValues;
                    this.eventId = eventId;
                }

                public void OnFormatted(string logEntry) => logger.Log(logEntry);

                public void OnFormatted<T0>(string originalFormat, T0 value0) => logger.Log(originalFormat, value0);

                public void OnFormatted<T0, T1>(string originalFormat, T0 value0, T1 value1) => logger.Log(originalFormat, value0, value1);

                public void OnFormatted<T0, T1, T2>(string originalFormat, T0 value0, T1 value1, T2 value2) => logger.Log(originalFormat, value0, value1, value2);

                public void OnFormatted<T0, T1, T2, T3>(string originalFormat, T0 value0, T1 value1, T2 value2, T3 value3) => logger.Log(originalFormat, value0, value1, value2, value3);

                public void OnFormatted<T0, T1, T2, T3, T4>(string originalFormat, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4) => logger.Log(originalFormat, value0, value1, value2, value3, value4);

                public void OnFormatted<T0, T1, T2, T3, T4, T5>(string originalFormat, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4,
                    T5 value5) =>
                    logger.Log(originalFormat, value0, value1, value2, value3, value4, value5);
            }

            interface IExternalLogger
            {
                void Log(string logEntry);
                void Log<T0>(string originalFormat, T0 value0);
                void Log<T0, T1>(string originalFormat, T0 value0, T1 value1);
                void Log<T0, T1, T2>(string originalFormat, T0 value0, T1 value1, T2 value2);
                void Log<T0, T1, T2, T3>(string originalFormat, T0 value0, T1 value1, T2 value2, T3 value3);
                void Log<T0, T1, T2, T3, T4>(string originalFormat, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4);
                void Log<T0, T1, T2, T3, T4, T5>(string originalFormat, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5);
            }

            class NoopExternalLogger : IExternalLogger
            {
                public void Log(string logEntry) { }

                public void Log<T0>(string originalFormat, T0 value0) { }

                public void Log<T0, T1>(string originalFormat, T0 value0, T1 value1) { }

                public void Log<T0, T1, T2>(string originalFormat, T0 value0, T1 value1, T2 value2) { }

                public void Log<T0, T1, T2, T3>(string originalFormat, T0 value0, T1 value1, T2 value2, T3 value3) { }

                public void Log<T0, T1, T2, T3, T4>(string originalFormat, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4) { }

                public void Log<T0, T1, T2, T3, T4, T5>(string originalFormat, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) { }
            }
        }
    }
}
