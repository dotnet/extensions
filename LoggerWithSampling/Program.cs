// Copyright (c) Microsoft Corporation. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
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
            return LoggerFactory.Create(builder =>
            {
                _ = builder.EnableSampling(samplingBuilder => samplingBuilder

                        // add the built-in simple sampler:
                        .SetSimpleSampler(o =>
                        {
                            o.Matchers = new List<Matcher>
                            {
                                new Matcher(
                                    new LogRecordPattern
                                    {
                                        LogLevel = LogLevel.Information,
                                        Category = "Microsoft.Extensions.Hosting",
                                    },
                                    (pattern) => Random.Shared.NextDouble() < 0.01),
                                new Matcher(
                                    new LogRecordPattern
                                    {
                                        LogLevel = LogLevel.Error,
                                    },
                                    (tool, pattern) => tool.Buffer("MyBuffer")),
                            };
                            o.Buffers = new HashSet<LogBuffer>
                            {
                                new LogBuffer
                                {
                                    Name = "MyBuffer",
                                    SuspendAfterFlushDuration = TimeSpan.FromSeconds(10),
                                    BufferingDuration = TimeSpan.FromSeconds(10),
                                    BufferSize = 1_000_000,
                                },
                            };
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
