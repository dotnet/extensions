// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Serilog;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Logging.Testing
{
    public class AssemblyTestLog : IDisposable
    {
        public static readonly string OutputDirectoryEnvironmentVariableName = "ASPNETCORE_TEST_LOG_DIR";

        private static readonly object _lock = new object();
        private static readonly Dictionary<Assembly, AssemblyTestLog> _logs = new Dictionary<Assembly, AssemblyTestLog>();

        private readonly ILoggerFactory _globalLoggerFactory;
        private readonly ILogger _globalLogger;
        private readonly string _baseDirectory;
        private readonly string _assemblyName;

        private AssemblyTestLog(ILoggerFactory globalLoggerFactory, ILogger globalLogger, string baseDirectory, string assemblyName)
        {
            _globalLoggerFactory = globalLoggerFactory;
            _globalLogger = globalLogger;
            _baseDirectory = baseDirectory;
            _assemblyName = assemblyName;
        }

        public IDisposable StartTestLog(ITestOutputHelper output, string className, out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null)
        {
            var factory = CreateLoggerFactory(output, className, testName);
            loggerFactory = factory;
            var logger = factory.CreateLogger("TestLifetime");

            var stopwatch = Stopwatch.StartNew();
            _globalLogger.LogInformation("Starting test {testName}", testName);
            logger.LogInformation("Starting test {testName}", testName);

            return new Disposable(() =>
            {
                stopwatch.Stop();
                _globalLogger.LogInformation("Finished test {testName} in {duration}s", testName, stopwatch.Elapsed.TotalSeconds);
                logger.LogInformation("Finished test {testName} in {duration}s", testName, stopwatch.Elapsed.TotalSeconds);
                factory.Dispose();
            });
        }

        public ILoggerFactory CreateLoggerFactory(ITestOutputHelper output, string className, [CallerMemberName] string testName = null)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder =>
            {
                if (output != null)
                {
                    builder.AddXunit(output, LogLevel.Debug);
                }
            });

            var loggerFactory = serviceCollection.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            // Try to shorten the class name using the assembly name
            if (className.StartsWith(_assemblyName + "."))
            {
                className = className.Substring(_assemblyName.Length + 1);
            }

            if (!string.IsNullOrEmpty(_baseDirectory))
            {
                var testOutputFile = Path.Combine(_baseDirectory, _assemblyName, className, $"{testName}.log");

                AddFileLogging(loggerFactory, testOutputFile);
            }

            return loggerFactory;
        }

        public static AssemblyTestLog Create(string assemblyName, string baseDirectory)
        {
            var serviceCollection = new ServiceCollection();

            // Let the global logger log to the console, it's just "Starting X..." "Finished X..."
            serviceCollection.AddLogging(builder => builder.AddConsole());

            var loggerFactory = serviceCollection.BuildServiceProvider().GetRequiredService<ILoggerFactory>();

            if (!string.IsNullOrEmpty(baseDirectory))
            {
                var globalLogFileName = Path.Combine(baseDirectory, assemblyName, "global.log");
                AddFileLogging(loggerFactory, globalLogFileName);
            }

            var logger = loggerFactory.CreateLogger("GlobalTestLog");
            logger.LogInformation($"Global Test Logging initialized. Set the '{OutputDirectoryEnvironmentVariableName}' Environment Variable in order to create log files on disk.");
            return new AssemblyTestLog(loggerFactory, logger, baseDirectory, assemblyName);
        }

        public static AssemblyTestLog ForAssembly(Assembly assembly)
        {
            lock (_lock)
            {
                if (!_logs.TryGetValue(assembly, out var log))
                {
                    log = Create(assembly.GetName().Name, Environment.GetEnvironmentVariable(OutputDirectoryEnvironmentVariableName));
                    _logs[assembly] = log;
                }
                return log;
            }
        }

        private static void AddFileLogging(ILoggerFactory loggerFactory, string fileName)
        {
            var dir = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            var serilogger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Verbose()
                .WriteTo.File(fileName, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{SourceContext}] [{Level}] {Message}{NewLine}{Exception}", flushToDiskInterval: TimeSpan.FromSeconds(1), shared: true)
                .CreateLogger();
            loggerFactory.AddSerilog(serilogger, dispose: true);
        }

        public void Dispose()
        {
            _globalLoggerFactory.Dispose();
        }

        private class Disposable : IDisposable
        {
            private Action _action;

            public Disposable(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();
            }
        }
    }
}
