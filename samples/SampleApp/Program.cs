// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Primitives;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SampleApp
{
    public class Program
    {
        private readonly ILogger _logger;

        public Program()
        {
            // A dependency injection based application would get ILoggerFactory injected instead.
            // Create a logger factory with filter settings that can be applied across all logger providers.
            var factory = new LoggerFactory()
                .WithFilter(new FilterLoggerSettings
                {
                    { "Microsoft", LogLevel.Warning },
                    { "System", LogLevel.Warning },
                    { "SampleApp.Program", LogLevel.Debug }
                });

            // getting the logger immediately using the class's name is conventional
            _logger = factory.CreateLogger<Program>();

            // providers may be added to an ILoggerFactory at any time, existing ILoggers are updated
#if !NETCOREAPP1_0
            factory.AddEventLog();
#endif

            // How to configure the console logger to reload based on a configuration file.
            //
            //
            var loggingConfiguration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("logging.json", optional: false, reloadOnChange: true)
                .Build();
            factory.AddConsole(loggingConfiguration);

            // How to configure the console logger to use settings provided in code.
            //
            //
            //var settings = new ConsoleLoggerSettings()
            //{
            //    IncludeScopes = true,
            //    Switches =
            //    {
            //        ["Default"] = LogLevel.Debug,
            //        ["Microsoft"] = LogLevel.Information,
            //    }
            //};
            //factory.AddConsole(settings);

            // How to manually wire up file-watching without a configuration file
            //
            //
            //factory.AddConsole(new RandomReloadingConsoleSettings());
        }

        private class RandomReloadingConsoleSettings : IConsoleLoggerSettings
        {
            private PhysicalFileProvider _files = new PhysicalFileProvider(PlatformServices.Default.Application.ApplicationBasePath);

            public RandomReloadingConsoleSettings()
            {
                Reload();
            }

            public IChangeToken ChangeToken { get; private set; }

            public bool IncludeScopes { get; }

            private Dictionary<string, LogLevel> Switches { get; set; }

            public IConsoleLoggerSettings Reload()
            {
                ChangeToken = _files.Watch("logging.json");
                Switches = new Dictionary<string, LogLevel>()
                {
                    ["Default"] = (LogLevel)(DateTimeOffset.Now.Second % 5 + 1),
                    ["Microsoft"] = (LogLevel)(DateTimeOffset.Now.Second % 5 + 1),
                };

                return this;
            }

            public bool TryGetSwitch(string name, out LogLevel level)
            {
                return Switches.TryGetValue(name, out level);
            }
        }

        public static void Main(string[] args)
        {
            new Program().Execute(args);
        }

        public void Execute(string[] args)
        {
            _logger.LogInformation("Starting");

            var startTime = DateTimeOffset.Now;
            _logger.LogInformation(1, "Started at '{StartTime}' and 0x{Hello:X} is hex of 42", startTime, 42);
            // or
            _logger.ProgramStarting(startTime, 42);

            using (_logger.PurchaseOrderScope("00655321"))
            {
                try
                {
                    throw new Exception("Boom");
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(1, ex, "Unexpected critical error starting application");
                    _logger.LogError(1, ex, "Unexpected error");
                    _logger.LogWarning(1, ex, "Unexpected warning");
                }

                using (_logger.BeginScope("Main"))
                {

                    _logger.LogInformation("Waiting for user input");

                    string input;
                    do
                    {
                        Console.WriteLine("Enter some test to log more, or 'quit' to exit.");
                        input = Console.ReadLine();

                        _logger.LogInformation("User typed '{input}' on the command line", input);
                        _logger.LogWarning("The time is now {Time}, it's getting late!", DateTimeOffset.Now);
                    }
                    while (input != "quit");
                }
            }

            var endTime = DateTimeOffset.Now;
            _logger.LogInformation(2, "Stopping at '{StopTime}'", endTime);
            // or
            _logger.ProgramStopping(endTime);

            _logger.LogInformation("Stopping");
        }
    }
}
