// Copyright (c) Microsoft Corporation. All Rights Reserved.

using System;
using Microsoft.Extensions.Diagnostics.Logging.Buffering;
using Microsoft.Extensions.Diagnostics.Logging.Sampling;
using Microsoft.Extensions.Logging;

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
            return LoggerFactory.Create(loggingBuilder =>
            {
c
            });
        }
                //loggingBuilder.AddScopedSampler(new MySampler());

                //loggingBuilder
                //    AddSampling()
                //AddHttpRequestBuffer
                //    .AddBuffering("bufferName", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), 1_000_000) //used and applied to everything by default
                //    //should be able to invert
                //    .AddBuffering(new Buffer(with options))
                //    .AddBuffering(name, timespan, options =>  { options.AddFilter(...); excludes.AddFilter(...)  })

                //    //add eventId filtering later
                //    // or accept a function to filter by eventId for AddSampling. for tags ??

                //    //

                ////align with AddFilter()
                //AddBuffering(excludes.AddFilter("Hosting", LogLevel));
                //AddHttpRequestBuffer(excludes.AddFilter("Hosting", LogLevel))
                //AddSampling((catg, LogLevel, eventId, state) => bool)
                //how much people care about state?
                //talk to people who use API
                //does it solve people's problem?'
                //AddFilter

                //    .AddSampler("samplerName", (category, loglevel, HttpContext context) => Random.Shared.NextDouble() < 0.01) // use composable sampler instead
                //    .AddStandardSampler1()
                //    ...

                //    .UseSampling("Microsoft.Extensions.Hosting", LogLevel.Information)
                //        .WithSampler(Constants.StandardSamplerName);

                //     .UseSampling("Microsoft.Extensions.Hosting", LogLevel.Information)
                //        .WithBuffer("bufferName"); // for request-based sampling try to use DI scope mechanism

                //.UseSomethingElse()
                //    .WithBuffer()



                //AddSampler(new ParenBasedSampler(..))

                //new ProbabilisticSampler(0.01)
                //.WithBuffer(

        private static void AssertNotNull(object? obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
        }
    }
}
