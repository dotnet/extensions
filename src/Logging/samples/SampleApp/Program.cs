// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SampleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var loggingConfiguration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("logging.json", optional: false, reloadOnChange: true)
                .Build();

            // A Web App based program would configure logging via the WebHostBuilder.
            // Create a logger factory with filters that can be applied across all logger providers.
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConfiguration(loggingConfiguration.GetSection("Logging"))
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("SampleApp.Program", LogLevel.Debug)
                    .AddConsole()
                    .AddEventLog();
            });

            // Make sure to dispose ILoggerFactory
            using (loggerFactory)
            {
                var logger = loggerFactory.CreateLogger<Program>();

                logger.LogInformation("Starting");

                var startTime = DateTimeOffset.Now;
                logger.LogInformation(1, "Started at '{StartTime}' and 0x{Hello:X} is hex of 42", startTime, 42);
                // or
                logger.ProgramStarting(startTime, 42);

                using (logger.PurchaseOrderScope("00655321"))
                {
                    try
                    {
                        throw new Exception("Boom");
                    }
                    catch (Exception ex)
                    {
                        logger.LogCritical(1, ex, "Unexpected critical error starting application");
                        logger.LogError(1, ex, "Unexpected error");
                        logger.LogWarning(1, ex, "Unexpected warning");
                    }

                    using (logger.BeginScope("Main"))
                    {

                        logger.LogInformation("Waiting for user input");

                        string input;
                        do
                        {
                            Console.WriteLine("Enter some test to log more, or 'quit' to exit.");
                            input = Console.ReadLine();

                            logger.LogInformation("User typed '{input}' on the command line", input);
                            logger.LogWarning("The time is now {Time}, it's getting late!", DateTimeOffset.Now);
                        }
                        while (input != "quit");
                    }
                }

                var endTime = DateTimeOffset.Now;
                logger.LogInformation(2, "Stopping at '{StopTime}'", endTime);
                // or
                logger.ProgramStopping(endTime);

                logger.LogInformation("Stopping");
            }
        }
    }
}
