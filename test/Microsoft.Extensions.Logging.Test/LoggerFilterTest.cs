// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class LoggerFilterTest
    {
        [Fact]
        public void FiltersMessages_OnDefaultLogLevel_BeforeSendingTo_AllRegisteredLoggerProviders()
        {
            // Arrange
            var loggerProvider1 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerProvider2 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerFactoryFromHost = new LoggerFactory();
            var filterLoggerFactory = loggerFactoryFromHost
                .WithFilter(new FilterLoggerSettings()
                {
                    { "Default", LogLevel.Warning }
                });
            filterLoggerFactory.AddProvider(loggerProvider1);
            filterLoggerFactory.AddProvider(loggerProvider2);
            var logger1 = loggerFactoryFromHost.CreateLogger("Microsoft.Foo");

            // Act
            logger1.LogCritical("critical event");
            logger1.LogDebug("debug event");
            logger1.LogInformation("information event");

            // Assert
            foreach (var sink in new[] { loggerProvider1.Sink, loggerProvider2.Sink })
            {
                var logEventWrites = sink.Writes.Where(wc => wc.LoggerName.StartsWith("Microsoft.Foo"));
                var logEventWrite = Assert.Single(logEventWrites);
                Assert.Equal("critical event", logEventWrite.State?.ToString());
                Assert.Equal(LogLevel.Critical, logEventWrite.LogLevel);
            }
        }

        [Fact]
        public void FiltersMessages_BeforeSendingTo_AllRegisteredLoggerProviders()
        {
            // Arrange
            var loggerProvider1 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerProvider2 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerFactoryFromHost = new LoggerFactory();
            var filterLoggerFactory = loggerFactoryFromHost
                .WithFilter(new FilterLoggerSettings()
                {
                        { "Microsoft", LogLevel.Warning },
                        { "System", LogLevel.Warning },
                        { "SampleApp", LogLevel.Debug },
                });
            filterLoggerFactory.AddProvider(loggerProvider1);
            filterLoggerFactory.AddProvider(loggerProvider2);
            var microsoftAssemblyLogger = loggerFactoryFromHost.CreateLogger("Microsoft.Foo");
            var systemAssemblyLogger = loggerFactoryFromHost.CreateLogger("System.Foo");
            var myappAssemblyLogger = loggerFactoryFromHost.CreateLogger("SampleApp.Program");

            // Act
            microsoftAssemblyLogger.LogCritical("critical event");
            microsoftAssemblyLogger.LogDebug("debug event");
            microsoftAssemblyLogger.LogInformation("information event");
            systemAssemblyLogger.LogCritical("critical event");
            systemAssemblyLogger.LogDebug("debug event");
            systemAssemblyLogger.LogInformation("information event");
            myappAssemblyLogger.LogCritical("critical event");
            myappAssemblyLogger.LogDebug("debug event");
            myappAssemblyLogger.LogInformation("information event");

            // Assert
            foreach (var sink in new[] { loggerProvider1.Sink, loggerProvider2.Sink })
            {
                var logEventWrites = sink.Writes.Where(wc => wc.LoggerName.StartsWith("Microsoft"));
                var logEventWrite = Assert.Single(logEventWrites);
                Assert.Equal(LogLevel.Critical, logEventWrite.LogLevel);
                Assert.Equal("critical event", logEventWrite.State?.ToString());

                logEventWrites = sink.Writes.Where(wc => wc.LoggerName.StartsWith("System"));
                logEventWrite = Assert.Single(logEventWrites);
                Assert.Equal(LogLevel.Critical, logEventWrite.LogLevel);
                Assert.Equal("critical event", logEventWrite.State?.ToString());

                logEventWrites = sink.Writes.Where(wc => wc.LoggerName.StartsWith("SampleApp.Program"));
                logEventWrite = Assert.Single(logEventWrites.Where(wc => wc.LogLevel == LogLevel.Critical));
                Assert.Equal("critical event", logEventWrite.State?.ToString());
                logEventWrite = Assert.Single(logEventWrites.Where(wc => wc.LogLevel == LogLevel.Debug));
                Assert.Equal("debug event", logEventWrite.State?.ToString());
                logEventWrite = Assert.Single(logEventWrites.Where(wc => wc.LogLevel == LogLevel.Information));
                Assert.Equal("information event", logEventWrite.State?.ToString());
            }
        }

        [Fact]
        public void BeginScope_CreatesScopesOn_AllRegisteredLoggerProviders()
        {
            // Arrange
            var loggerProvider1 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerProvider2 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerFactoryFromHost = new LoggerFactory();
            var filterLoggerFactory = loggerFactoryFromHost
                .WithFilter(new FilterLoggerSettings()
                {
                    { "Microsoft", LogLevel.Warning },
                    { "System", LogLevel.Warning },
                    { "SampleApp", LogLevel.Debug },
                });
            filterLoggerFactory.AddProvider(loggerProvider1);
            filterLoggerFactory.AddProvider(loggerProvider2);
            var microsoftAssemblyLogger = loggerFactoryFromHost.CreateLogger("Microsoft.foo");
            var systemAssemblyLogger = loggerFactoryFromHost.CreateLogger("System.foo");
            var myappAssemblyLogger = loggerFactoryFromHost.CreateLogger("SampleApp.Program");

            // Act
            var disposable1 = systemAssemblyLogger.BeginScope("Scope1");
            var disposable2 = microsoftAssemblyLogger.BeginScope("Scope2");
            var disposable3 = myappAssemblyLogger.BeginScope("Scope3");

            // Assert
            foreach (var sink in new[] { loggerProvider1.Sink, loggerProvider2.Sink })
            {
                var scopeContexts = sink.Scopes;
                Assert.Equal(3, scopeContexts.Count);

                Assert.Equal("Scope1", scopeContexts[0].Scope?.ToString());
                Assert.NotNull(disposable1);

                Assert.Equal("Scope2", scopeContexts[1].Scope?.ToString());
                Assert.NotNull(disposable2);

                Assert.Equal("Scope3", scopeContexts[2].Scope?.ToString());
                Assert.NotNull(disposable3);
            }
        }

        [Fact]
        public void DisposeOnFilterLoggerFactory_DoesNotCallDisposeOn_AllRegisteredLoggerProviders()
        {
            // Arrange
            var loggerProvider1 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerProvider2 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerFactoryFromHost = new LoggerFactory();
            var filterLoggerFactory = loggerFactoryFromHost
                .WithFilter(new FilterLoggerSettings()
                {
                    { "Microsoft", LogLevel.Warning },
                    { "System", LogLevel.Warning },
                    { "SampleApp", LogLevel.Debug },
                });
            filterLoggerFactory.AddProvider(loggerProvider1);
            filterLoggerFactory.AddProvider(loggerProvider2);
            var logger1 = loggerFactoryFromHost.CreateLogger("Microsoft.foo");

            // Act
            filterLoggerFactory.Dispose();

            // Assert
            Assert.False(loggerProvider1.DisposeCalled);
            Assert.False(loggerProvider2.DisposeCalled);
        }

        [Fact]
        public void DisposeOnLoggerFactory_CallsDisposeOn_AllRegisteredLoggerProviders()
        {
            // Arrange
            var loggerProvider1 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerProvider2 = new TestLoggerProvider(new TestSink(), isEnabled: true);

            // Imagine this to be the default logger factory that is provided by the host and is
            // present in DI.
            var loggerFactoryFromHost = new LoggerFactory();

            // Imagine this to be the user code which adds the wrapped logger providers.
            var filterLoggerFactory = loggerFactoryFromHost
                .WithFilter(new FilterLoggerSettings()
                {
                    { "Microsoft", LogLevel.Warning },
                    { "System", LogLevel.Warning },
                    { "SampleApp", LogLevel.Debug },
                });
            filterLoggerFactory.AddProvider(loggerProvider1);
            filterLoggerFactory.AddProvider(loggerProvider2);
            var logger1 = loggerFactoryFromHost.CreateLogger("Microsoft.foo");

            // Act
            loggerFactoryFromHost.Dispose();

            // Assert
            Assert.True(loggerProvider1.DisposeCalled);
            Assert.True(loggerProvider2.DisposeCalled);
        }

        [Fact]
        public void CanFilterMessagesAtProviderLevel_AfterFilterLoggerFactory_HasFilteredMessages()
        {
            // Arrange
            var loggerProvider1 = new TestLoggerProvider(new TestSink(), filter: level => level == LogLevel.Critical);
            var loggerProvider2 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerFactoryFromHost = new LoggerFactory();
            var filterLoggerFactory = loggerFactoryFromHost
                .WithFilter(new FilterLoggerSettings()
                {
                    { "Default", LogLevel.Warning }
                });
            filterLoggerFactory.AddProvider(loggerProvider1);
            filterLoggerFactory.AddProvider(loggerProvider2);
            var logger = loggerFactoryFromHost.CreateLogger("Microsoft.Foo");

            // Act
            logger.LogCritical("critical event");
            logger.LogWarning("warning event");
            logger.LogTrace("trace event");

            // Assert
            // This provider filters the messages further to only log 'critical' messages
            var sink1 = loggerProvider1.Sink;
            var logEventWrites = sink1.Writes.Where(wc => wc.LoggerName.Equals("Microsoft.Foo")).ToList();
            Assert.Equal(1, logEventWrites.Count);
            Assert.Equal("critical event", logEventWrites[0].State?.ToString());
            Assert.Equal(LogLevel.Critical, logEventWrites[0].LogLevel);

            var sink2 = loggerProvider2.Sink;
            logEventWrites = sink2.Writes.Where(wc => wc.LoggerName.Equals("Microsoft.Foo")).ToList();
            Assert.Equal(2, logEventWrites.Count);
            Assert.Equal("critical event", logEventWrites[0].State?.ToString());
            Assert.Equal(LogLevel.Critical, logEventWrites[0].LogLevel);
            Assert.Equal("warning event", logEventWrites[1].State?.ToString());
            Assert.Equal(LogLevel.Warning, logEventWrites[1].LogLevel);
        }
    }
}
