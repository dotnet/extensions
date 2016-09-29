// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging.AzureWebAppDiagnostics.Internal;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Extensions.Logging.Azure.AppServicesDiagnostics.Test
{
    public class LogConfigurationReaderTests
    {
        private readonly int DefaultTimeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;

        [Fact]
        public void OutsideOfWebAppTheConfigurationIsDisabled()
        {
            var contextMock = new Mock<IWebAppContext>(MockBehavior.Strict);
            contextMock.SetupGet(c => c.IsRunningInAzureWebApp).Returns(false);

            var configReader = new WebAppLogConfigurationReader(contextMock.Object);

            Assert.Same(WebAppLogConfiguration.Disabled, configReader.Current);
        }

        [Fact]
        public void NoConfigFile()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "AzureWebAppLoggerThisFolderShouldNotExist");
            var logFolder = Path.Combine(tempFolder, "LogFiles", "Application");

            var contextMock = new Mock<IWebAppContext>();
            contextMock.SetupGet(c => c.IsRunningInAzureWebApp)
                .Returns(true);

            contextMock.SetupGet(c => c.HomeFolder)
                .Returns(tempFolder);

            using (var configReader = new WebAppLogConfigurationReader(contextMock.Object))
            {
                var config = configReader.Current;

                Assert.True(config.IsRunningInWebApp);

                Assert.False(config.FileLoggingEnabled);
                Assert.Equal(LogLevel.None, config.FileLoggingLevel);
                Assert.Equal(logFolder, config.FileLoggingFolder);

                Assert.False(config.BlobLoggingEnabled);
                Assert.Equal(LogLevel.None, config.BlobLoggingLevel);
                Assert.Null(config.BlobContainerUrl);
            }
        }

        [Fact]
        public void ConfigurationDisabledInSettingsFile()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "WebAppLoggerConfigurationDisabledInSettingsFile");

            try
            {
                var logFolder = Path.Combine(tempFolder, "LogFiles", "Application");

                var settingsFolder = Path.Combine(tempFolder, "site", "diagnostics");
                var settingsFile = Path.Combine(settingsFolder, "settings.json");

                if (!Directory.Exists(settingsFolder))
                {
                    Directory.CreateDirectory(settingsFolder);
                }

                var settingsFileContent = new SettingsFileContent
                {
                    AzureDriveEnabled = false,
                    AzureDriveTraceLevel = "Verbose",

                    AzureBlobEnabled = false,
                    AzureBlobTraceLevel = "Error"
                };

                File.WriteAllText(settingsFile, JsonConvert.SerializeObject(settingsFileContent));

                var contextMock = new Mock<IWebAppContext>();
                contextMock.SetupGet(c => c.IsRunningInAzureWebApp)
                    .Returns(true);
                contextMock.SetupGet(c => c.HomeFolder)
                    .Returns(tempFolder);

                using (var configReader = new WebAppLogConfigurationReader(contextMock.Object))
                {
                    var config = configReader.Current;

                    Assert.False(config.FileLoggingEnabled);
                    Assert.Equal(LogLevel.Trace, config.FileLoggingLevel);

                    Assert.False(config.BlobLoggingEnabled);
                    Assert.Equal(LogLevel.Error, config.BlobLoggingLevel);
                }
            }
            finally
            {
                if (Directory.Exists(tempFolder))
                {
                    try
                    {
                        Directory.Delete(tempFolder, recursive: true);
                    }
                    catch
                    {
                        // Don't break the test if temp folder deletion fails.
                    }
                }
            }
        }

        [Fact]
        public void ConfigurationChange()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "WebAppLoggerConfigurationChange");

            try
            {
                var logFolder = Path.Combine(tempFolder, "LogFiles", "Application");

                var settingsFolder = Path.Combine(tempFolder, "site", "diagnostics");
                var settingsFile = Path.Combine(settingsFolder, "settings.json");

                if (!Directory.Exists(settingsFolder))
                {
                    Directory.CreateDirectory(settingsFolder);
                }

                var settingsFileContent = new SettingsFileContent
                {
                    AzureDriveEnabled = false,
                    AzureDriveTraceLevel = "Verbose",

                    AzureBlobEnabled = false,
                    AzureBlobTraceLevel = "Error"
                };

                File.WriteAllText(settingsFile, JsonConvert.SerializeObject(settingsFileContent));

                var contextMock = new Mock<IWebAppContext>();
                contextMock.SetupGet(c => c.IsRunningInAzureWebApp)
                    .Returns(true);
                contextMock.SetupGet(c => c.HomeFolder)
                    .Returns(tempFolder);

                using (var configChangedEvent = new ManualResetEvent(false))
                using (var configReader = new WebAppLogConfigurationReader(contextMock.Object))
                {
                    WebAppLogConfiguration config = null;

                    configReader.OnConfigurationChanged += (sender, newConfig) =>
                    {
                        config = newConfig;

                        try
                        {
                            configChangedEvent.Set();
                        }
                        catch (ObjectDisposedException)
                        {
                            // This can happen if the file watcher triggers multiple times
                            // and there are in flight events that run after we dispose
                            // the manual reset event. Same issue as in dotnet-watch
                        }
                    };

                    // Wait 1 second because on unix the file time resolution is 1s and the watcher might not fire
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    settingsFileContent.AzureBlobEnabled = true;
                    settingsFileContent.AzureDriveTraceLevel = "Information";

                    RetryHelper.Retry(() =>
                    {
                        // The .NET file watcher doesn't trigger all the times. We might have to retry the operation
                        File.WriteAllText(settingsFile, JsonConvert.SerializeObject(settingsFileContent));
                        return configChangedEvent.WaitOne(DefaultTimeout);
                    });

                    Assert.Same(config, configReader.Current);

                    Assert.False(config.FileLoggingEnabled);
                    Assert.Equal(LogLevel.Information, config.FileLoggingLevel);

                    Assert.True(config.BlobLoggingEnabled);
                    Assert.Equal(LogLevel.Error, config.BlobLoggingLevel);
                }
            }
            finally
            {
                if (Directory.Exists(tempFolder))
                {
                    try
                    {
                        Directory.Delete(tempFolder, recursive: true);
                    }
                    catch
                    {
                        // Don't break the test if temp folder deletion fails.
                    }
                }
            }
        }
    }
}