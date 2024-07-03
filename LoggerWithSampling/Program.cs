// Copyright (c) Microsoft Corporation. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Microsoft.Extensions.Diagnostics.Logging.Buffering;
using Microsoft.Extensions.Diagnostics.Logging.Sampling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsoleLogger
{
    internal static class Program
    {
        private static void Main()
        {
            LoggerDemo("Structured Logging", formatMessage: false);
            LoggerDemo("Unstructured Logging", formatMessage: true);
        }

        private static void LoggerDemo(string loggerCategory, bool formatMessage)
        {
            Console.WriteLine($"*** {loggerCategory} demo ***");

            using ILoggerFactory loggerFactory = CreateLoggerFactory(formatMessage);
            ILogger logger = loggerFactory.CreateLogger(loggerCategory);

            logger.LearnMoreAt("R9 logger", "aka.ms/r9");

            using (logger.BeginScope("Exception demo"))
            {
                try
                {
                    AssertNotNull(null);
                }
                catch (ArgumentNullException e)
                {
                    logger.LogArgumentNullException(e);
                }
            }

            Console.WriteLine();
        }

        private static ILoggerFactory CreateLoggerFactory(bool formatMessage)
        {
            const string myGlobalBufferName = "My buffer";
            return LoggerFactory.Create(builder =>
            {
                builder
                    .EnableBuffering(opts =>
                    {
                        opts.Configs.Add(new LogBufferConfig
                        {
                            Name = myGlobalBufferName,
                            SuspendAfterFlushDuration = TimeSpan.FromSeconds(10),
                            Duration = TimeSpan.FromSeconds(10),
                            Size = 1_000_000,
                        });
                    })
                    .EnableSampling(samplingBuilder => samplingBuilder
                        .EnableSimpleSamplingFilter(opts =>
                        {
                            opts.Matchers.Add(
                                new SamplingMatcher(
                                    new LogRecordPattern
                                    {
                                        LogLevel = LogLevel.Information,
                                        Category = "Microsoft.Extensions.Hosting",
                                    },
                                    (pattern) => Random.Shared.NextDouble() < 0.01));
                        })
                        .EnableSimpleBufferingFilter(opts =>
                        {
                            opts.Matchers.Add(
                                new BufferingMatcher(
                                    new LogRecordPattern
                                    {
                                        LogLevel = LogLevel.Error,
                                    },
                                    (tool, pattern) => tool.Buffer(myGlobalBufferName, pattern)));
                        }));
            });
        }

        private static void AssertNotNull(object? obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
        }
    }
}
