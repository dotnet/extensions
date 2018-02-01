// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;

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
        private readonly IServiceProvider _serviceProvider;

        private AssemblyTestLog(ILoggerFactory globalLoggerFactory, ILogger globalLogger, string baseDirectory, string assemblyName, IServiceProvider serviceProvider)
        {
            _globalLoggerFactory = globalLoggerFactory;
            _globalLogger = globalLogger;
            _baseDirectory = baseDirectory;
            _assemblyName = assemblyName;
            _serviceProvider = serviceProvider;
        }

        public IDisposable StartTestLog(ITestOutputHelper output, string className, out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null) =>
            StartTestLog(output, className, out loggerFactory, LogLevel.Debug, testName);

        public IDisposable StartTestLog(ITestOutputHelper output, string className, out ILoggerFactory loggerFactory, LogLevel minLogLevel, [CallerMemberName] string testName = null)
        {
            var serviceProvider = CreateLoggerServices(output, className, minLogLevel, testName);
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            loggerFactory = factory;
            var logger = loggerFactory.CreateLogger("TestLifetime");

            var stopwatch = Stopwatch.StartNew();

            var scope = logger.BeginScope("Test: {testName}", testName);

            _globalLogger.LogInformation("Starting test {testName}", testName);
            logger.LogInformation("Starting test {testName}", testName);

            return new Disposable(() =>
            {
                stopwatch.Stop();
                _globalLogger.LogInformation("Finished test {testName} in {duration}s", testName, stopwatch.Elapsed.TotalSeconds);
                logger.LogInformation("Finished test {testName} in {duration}s", testName, stopwatch.Elapsed.TotalSeconds);
                scope.Dispose();
                factory.Dispose();
                (serviceProvider as IDisposable)?.Dispose();
            });
        }

        public ILoggerFactory CreateLoggerFactory(ITestOutputHelper output, string className, [CallerMemberName] string testName = null) =>
            CreateLoggerFactory(output, className, LogLevel.Trace, testName);

        public ILoggerFactory CreateLoggerFactory(ITestOutputHelper output, string className, LogLevel minLogLevel, [CallerMemberName] string testName = null)
        {
            return CreateLoggerServices(output, className, minLogLevel, testName).GetRequiredService<ILoggerFactory>();
        }

        public IServiceProvider CreateLoggerServices(ITestOutputHelper output, string className, LogLevel minLogLevel, [CallerMemberName] string testName = null)
        {
            // Try to shorten the class name using the assembly name
            if (className.StartsWith(_assemblyName + "."))
            {
                className = className.Substring(_assemblyName.Length + 1);
            }

            SerilogLoggerProvider serilogLoggerProvider = null;
            if (!string.IsNullOrEmpty(_baseDirectory))
            {
                var testOutputFile = Path.Combine(_baseDirectory, _assemblyName, RuntimeInformation.FrameworkDescription.TrimStart('.'), className, $"{testName}.log");

                serilogLoggerProvider = ConfigureFileLogging(testOutputFile);
            }

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder =>
            {
                builder.SetMinimumLevel(minLogLevel);

                if (output != null)
                {
                    builder.AddXunit(output, minLogLevel);
                }

                if (serilogLoggerProvider != null)
                {
                    // Use a factory so that the container will dispose it
                    builder.Services.AddSingleton<ILoggerProvider>(_ => serilogLoggerProvider);
                }
            });

            return serviceCollection.BuildServiceProvider();
        }

        public static AssemblyTestLog Create(string assemblyName, string baseDirectory)
        {
            SerilogLoggerProvider serilogLoggerProvider = null;
            if (!string.IsNullOrEmpty(baseDirectory))
            {
                var globalLogFileName = Path.Combine(baseDirectory, assemblyName, RuntimeInformation.FrameworkDescription.TrimStart('.'), "global.log");
                serilogLoggerProvider = ConfigureFileLogging(globalLogFileName);
            }

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(builder =>
            {
                // Global logging, when it's written, is expected to be outputted. So set the log level to minimum.
                builder.SetMinimumLevel(LogLevel.Trace);

                if (serilogLoggerProvider != null)
                {
                    // Use a factory so that the container will dispose it
                    builder.Services.AddSingleton<ILoggerProvider>(_ => serilogLoggerProvider);
                }
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            var logger = loggerFactory.CreateLogger("GlobalTestLog");
            logger.LogInformation($"Global Test Logging initialized. Set the '{OutputDirectoryEnvironmentVariableName}' Environment Variable in order to create log files on disk.");
            return new AssemblyTestLog(loggerFactory, logger, baseDirectory, assemblyName, serviceProvider);
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

        private static SerilogLoggerProvider ConfigureFileLogging(string fileName)
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
            return new SerilogLoggerProvider(serilogger, dispose: true);
        }

        public void Dispose()
        {
            (_serviceProvider as IDisposable)?.Dispose();
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
