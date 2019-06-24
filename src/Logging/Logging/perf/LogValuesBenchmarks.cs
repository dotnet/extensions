// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Logging.Performance
{
    [AspNetCoreBenchmark]
    public class FormattingBenchmarks : LoggingBenchmarkBase
    {
        ILogger formattingLogger;
        ILogger rawLogger;

        [Benchmark]
        public void TwoArguments_Formatting()
        {
            TwoArgumentErrorMessage(formattingLogger, 1, "string", Exception);
        }

        [Benchmark]
        public void TwoArguments_Raw()
        {
            TwoArgumentErrorMessage(rawLogger, 1, "string", Exception);
        }

        [Benchmark]
        public void TwoArguments_NoEx_Formatting()
        {
            TwoArgumentErrorMessage(formattingLogger, 1, "string", null);
        }

        [Benchmark]
        [DisassemblyDiagnoser]
        public void TwoArguments_NoEx_Raw()
        {
            TwoArgumentErrorMessage(rawLogger, 1, "string", null);
        }

        [Benchmark(Baseline = true)]
        public void NoArguments()
        {
            NoArgumentErrorMessage(formattingLogger, Exception);
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            formattingLogger = CreateLogger<MessageFormattingLogger>();
            rawLogger = CreateLogger<MessageRawLogger>();
        }

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
        }

        class MessageRawLogger : ILogger
        {
            readonly ITypedLogger logger;

            public MessageRawLogger()
            {
                logger = new NoopExternalLogger();
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (exception != null)
                {
                    formatter(state, exception);
                    return;
                }

                if (state is ILogValues)
                {
                    ((ILogValues)state).Log(logLevel, eventId, logger);
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

            class NoopExternalLogger : ITypedLogger
            {
                public void Log(LogLevel logLevel, EventId eventId, string message)
                {
                }

                public void Log<T0>(LogLevel logLevel, EventId eventId, string originalFormat, T0 value0)
                {
                }

                public void Log<T0, T1>(LogLevel logLevel, EventId eventId, string originalFormat, T0 value0, T1 value1)
                {
                }

                public void Log<T0, T1, T2>(LogLevel logLevel, EventId eventId, string originalFormat, T0 value0, T1 value1, T2 value2)
                {
                }

                public void Log<T0, T1, T2, T3>(LogLevel logLevel, EventId eventId, string originalFormat, T0 value0, T1 value1, T2 value2,
                    T3 value3)
                {
                }

                public void Log<T0, T1, T2, T3, T4>(LogLevel logLevel, EventId eventId, string originalFormat, T0 value0, T1 value1, T2 value2,
                    T3 value3, T4 value4)
                {
                }

                public void Log<T0, T1, T2, T3, T4, T5>(LogLevel logLevel, EventId eventId, string originalFormat, T0 value0, T1 value1,
                    T2 value2, T3 value3, T4 value4, T5 value5)
                {
                }
            }
        }
    }
}
