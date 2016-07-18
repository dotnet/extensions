// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging.AzureWebAppDiagnostics.Internal;
using Moq;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Microsoft.Extensions.Logging.AzureWebAppDiagnostics.Test
{
    public class SerilogLoggerProviderTests
    {
        [Fact]
        public void OnStartDisable()
        {
            var configReader = new Mock<IWebAppLogConfigurationReader>();
            configReader.SetupGet(m => m.Current).Returns(WebAppLogConfiguration.Disabled);

            // Nothing should be called on this object
            var testSink = new Mock<ILogEventSink>(MockBehavior.Strict);

            using (var provider = new TestWebAppSerilogLoggerProvider(testSink.Object, configReader.Object))
            {
                var logger = provider.CreateLogger("TestLogger");
                logger.LogInformation("Test");
            }
        }

        [Fact]
        public void OnStartLoggingLevel()
        {
            var config = new WebAppLogConfigurationBuilder()
                    .SetIsRunningInAzureWebApps(true)
                    .SetFileLoggingEnabled(true)
                    .SetFileLoggingLevel(LogLevel.Information)
                    .Build();

            var configReader = new Mock<IWebAppLogConfigurationReader>();
            configReader.SetupGet(m => m.Current).Returns(config);
                
            var testSink = new Mock<ILogEventSink>(MockBehavior.Strict);
            testSink.Setup(m => m.Emit(It.IsAny<LogEvent>()));

            using (var provider = new TestWebAppSerilogLoggerProvider(testSink.Object, configReader.Object))
            {
                var logger = provider.CreateLogger("TestLogger");
                logger.LogInformation("Test");
            }

            testSink.Verify(m => m.Emit(It.IsAny<LogEvent>()), Times.Once);
        }

        [Fact]
        public void DynamicDisable()
        {
            var configBuilder = new WebAppLogConfigurationBuilder()
                    .SetIsRunningInAzureWebApps(true)
                    .SetFileLoggingEnabled(true)
                    .SetFileLoggingLevel(LogLevel.Information);

            var currentConfig = configBuilder.Build();

            var configReader = new Mock<IWebAppLogConfigurationReader>();
            configReader.SetupGet(m => m.Current)
                .Returns(() => { return currentConfig; });

            var testSink = new Mock<ILogEventSink>(MockBehavior.Strict);
            testSink.Setup(m => m.Emit(It.IsAny<LogEvent>()));

            using (var provider = new TestWebAppSerilogLoggerProvider(testSink.Object, configReader.Object))
            {
                var logger = provider.CreateLogger("TestLogger");

                logger.LogInformation("Test1");
                testSink.Verify(m => m.Emit(It.IsAny<LogEvent>()), Times.Once);

                configBuilder.SetFileLoggingEnabled(false);
                currentConfig = configBuilder.Build();

                configReader.Raise(m => m.OnConfigurationChanged += (sender, e) => { }, null, currentConfig);

                // Logging should be disabled now
                logger.LogInformation("Test1");
                testSink.Verify(m => m.Emit(It.IsAny<LogEvent>()), Times.Once);
            }
        }

        [Fact]
        public void DynamicLoggingLevel()
        {
            var configBuilder = new WebAppLogConfigurationBuilder()
                     .SetIsRunningInAzureWebApps(true)
                     .SetFileLoggingEnabled(true)
                     .SetFileLoggingLevel(LogLevel.Critical);

            var currentConfig  = configBuilder.Build();

            var configReader = new Mock<IWebAppLogConfigurationReader>();
            configReader.SetupGet(m => m.Current)
                .Returns(() => { return currentConfig; });

            var testSink = new Mock<ILogEventSink>(MockBehavior.Strict);
            testSink.Setup(m => m.Emit(It.IsAny<LogEvent>()));

            using (var provider = new TestWebAppSerilogLoggerProvider(testSink.Object, configReader.Object))
            {
                var logger = provider.CreateLogger("TestLogger");

                logger.LogDebug("Test1");
                testSink.Verify(m => m.Emit(It.IsAny<LogEvent>()), Times.Never);

                configBuilder.SetFileLoggingLevel(LogLevel.Debug);
                currentConfig = configBuilder.Build();

                configReader.Raise(m => m.OnConfigurationChanged += (sender, e) => { }, null, currentConfig);

                // Logging for this level should be enabled now
                logger.LogDebug("Test1");
                testSink.Verify(m => m.Emit(It.IsAny<LogEvent>()), Times.Once);
            }
        }

        // Checks that the .net log level to serilog level mappings are doing what we expect
        [Fact]
        public void LevelMapping()
        {
            var configBuilder = new WebAppLogConfigurationBuilder()
                     .SetIsRunningInAzureWebApps(true)
                     .SetFileLoggingEnabled(true);

            var currentConfig = configBuilder.Build();

            var configReader = new Mock<IWebAppLogConfigurationReader>();
            configReader.SetupGet(m => m.Current)
                .Returns(() => { return currentConfig; });

            var testSink = new Mock<ILogEventSink>(MockBehavior.Strict);
            testSink.Setup(m => m.Emit(It.IsAny<LogEvent>()));

            using (var provider = new TestWebAppSerilogLoggerProvider(testSink.Object, configReader.Object))
            {
                var levelsToCheck = new LogLevel[] {
                    LogLevel.None,
                    LogLevel.Critical,
                    LogLevel.Error,
                    LogLevel.Warning,
                    LogLevel.Information,
                    LogLevel.Debug,
                    LogLevel.Trace
                };

                var logger = provider.CreateLogger("TestLogger");

                for (int i = 0; i < levelsToCheck.Length; i++)
                {
                    var enabledLevel = levelsToCheck[i];

                    // Change the logging level
                    configBuilder.SetFileLoggingLevel(enabledLevel);
                    currentConfig = configBuilder.Build();
                    configReader.Raise(m => m.OnConfigurationChanged += (sender, e) => { }, null, currentConfig);

                    // Don't try to log "None" (start at 1)
                    for (int j = 1; j < levelsToCheck.Length; j++)
                    {
                        logger.Log(levelsToCheck[j], 1, new object(), null, (state, ex) => string.Empty);
                    }

                    // On each level we expect an extra message from the previous
                    testSink.Verify(
                        m => m.Emit(It.IsAny<LogEvent>()), 
                        Times.Exactly(i),
                        $"Enabled level: {enabledLevel}");

                    testSink.ResetCalls();
                }
            }
        }


        private class TestWebAppSerilogLoggerProvider : SerilogLoggerProvider
        {
            public TestWebAppSerilogLoggerProvider(ILogEventSink sink, IWebAppLogConfigurationReader configReader) :
                base(configReader,
                    (logger, config) =>
                    {
                        logger.WriteTo.Sink(sink);
                    })
            {
            }

            protected override void OnConfigurationChanged(WebAppLogConfiguration newConfiguration)
            {
                if (!newConfiguration.FileLoggingEnabled)
                {
                    LevelSwitcher.MinimumLevel = LogLevelDisabled;
                }
                else
                {
                    LevelSwitcher.MinimumLevel = LogLevelToLogEventLevel(newConfiguration.FileLoggingLevel);
                }
            }
        }
    }
}
