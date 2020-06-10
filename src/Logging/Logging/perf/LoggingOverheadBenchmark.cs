// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Logging.Performance
{
    [AspNetCoreBenchmark]
    public class LoggingOverheadBenchmark: LoggingBenchmarkBase
    {
        private ILogger _logger;

        [Benchmark]
        public void NoArguments_FilteredByLevel()
        {
            _logger.LogTrace(Exception, "Message");
        }

        // Baseline as this is the fastest way to do nothing
        [Benchmark(Baseline = true)]
        public void NoArguments_DefineMessage_FilteredByLevel()
        {
            NoArgumentTraceMessage(_logger, Exception);
        }

        [Benchmark]
        public void NoArguments()
        {
            _logger.LogError(Exception, "Message");
        }

        [Benchmark]
        public void NoArguments_DefineMessage()
        {
            NoArgumentErrorMessage(_logger, Exception);
        }

        [Benchmark]
        public void TwoArguments()
        {
            _logger.LogError(Exception, "Message {Argument1} {Argument2}", 1, "string");
        }

        [Benchmark]
        public void TwoArguments_FilteredByLevel()
        {
            _logger.LogTrace(Exception, "Message {Argument1} {Argument2}", 1, "string");
        }

        [Benchmark]
        public void TwoArguments_DefineMessage()
        {
            TwoArgumentErrorMessage(_logger, 1, "string", Exception);
        }

        [Benchmark]
        public void TwoArguments_DefineMessage_FilteredByLevel()
        {
            TwoArgumentTraceMessage(_logger, 1, "string", Exception);
        }

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ILoggerProvider, LoggerProvider<NoopLogger>>();
            _logger = services.BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger("Logger");
        }
    }
}
