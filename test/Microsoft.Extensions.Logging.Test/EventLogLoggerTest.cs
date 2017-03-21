// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET452
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.Logging.EventLog.Internal;
using Xunit;

namespace Microsoft.Extensions.Logging
{
    public class EventLogLoggerTest
    {
        [Fact]
        public void CallingBeginScopeOnLogger_ReturnsNonNullableInstance()
        {
            // Arrange
            var logger = new EventLogLogger("Test");

            // Act
            var disposable = logger.BeginScope("Scope1");

            // Assert
            Assert.NotNull(disposable);
        }

        [Fact]
        public void WindowsEventLog_Constructor_CreatesWithExpectedInformation()
        {
            // Arrange
            var logName = "foo";
            var machineName = "bar";
            var sourceName = "blah";

            // Act
            var windowsEventLog = new WindowsEventLog(logName, machineName, sourceName);

            // Assert
            Assert.NotNull(windowsEventLog.DiagnosticsEventLog);
            Assert.Equal(logName, windowsEventLog.DiagnosticsEventLog.Log);
            Assert.Equal(machineName, windowsEventLog.DiagnosticsEventLog.MachineName);
            Assert.Equal(sourceName, windowsEventLog.DiagnosticsEventLog.Source);
        }

        [Fact]
        public void Constructor_CreatesWindowsEventLog_WithExpectedInformation()
        {
            // Arrange & Act
            var eventLogLogger = new EventLogLogger("Test");

            // Assert
            var windowsEventLog = Assert.IsType<WindowsEventLog>(eventLogLogger.EventLog);
            Assert.Equal("Application", windowsEventLog.DiagnosticsEventLog.Log);
            Assert.Equal("Application", windowsEventLog.DiagnosticsEventLog.Source);
            Assert.Equal(".", windowsEventLog.DiagnosticsEventLog.MachineName);
        }

        [Fact]
        public void Constructor_CreatesWindowsEventLog_WithSuppliedEventLogSettings()
        {
            // Arrange
            var settings = new EventLogSettings()
            {
                SourceName = "foo",
                LogName = "bar",
                MachineName = "blah",
                EventLog = null
            };

            // Act
            var eventLogLogger = new EventLogLogger("Test", settings);

            // Assert
            var windowsEventLog = Assert.IsType<WindowsEventLog>(eventLogLogger.EventLog);
            Assert.Equal(settings.LogName, windowsEventLog.DiagnosticsEventLog.Log);
            Assert.Equal(settings.SourceName, windowsEventLog.DiagnosticsEventLog.Source);
            Assert.Equal(settings.MachineName, windowsEventLog.DiagnosticsEventLog.MachineName);
        }

        [Theory]
        [InlineData(50)]
        [InlineData(49)]
        [InlineData(36)]
        public void MessageWithinMaxSize_WritesFullMessage(int messageSize)
        {
            // Arrange
            var loggerName = "Test";
            var maxMessageSize = 50 + loggerName.Length + Environment.NewLine.Length;
            var message = new string('a', messageSize);
            var expectedMessage = loggerName + Environment.NewLine + message;
            var testEventLog = new TestEventLog(maxMessageSize);
            var logger = new EventLogLogger(loggerName, new EventLogSettings() { EventLog = testEventLog });

            // Act
            logger.LogInformation(message);

            // Assert
            Assert.Equal(1, testEventLog.Messages.Count);
            Assert.Equal(expectedMessage, testEventLog.Messages[0]);
        }

        public static TheoryData<int, string[]> WritesSplitMessagesData
        {
            get
            {
                var loggerName = "Test";

                return new TheoryData<int, string[]>
                {
                    // loggername + newline combined length is 7
                    {
                        1,
                        new[]
                        {
                            loggerName + Environment.NewLine + "a"
                        }
                    },
                    {
                        5,
                        new[]
                        {
                            loggerName + Environment.NewLine + "a...",
                            "...aaaa"
                        }
                    },
                    {
                        10, // equaling the max message size
                        new[]
                        {
                            loggerName + Environment.NewLine + "a...",
                            "...aaaa...",
                            "...aaaaa"
                        }
                    },
                    {
                        15,
                        new[]
                        {
                            loggerName + Environment.NewLine + "a...",
                            "...aaaa...",
                            "...aaaa...",
                            "...aaaaaa"
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(WritesSplitMessagesData))]
        public void MessageExceedingMaxSize_WritesSplitMessages(int messageSize, string[] expectedMessages)
        {
            // Arrange
            var loggerName = "Test";
            var maxMessageSize = 10;
            var message = new string('a', messageSize);
            var testEventLog = new TestEventLog(maxMessageSize);
            var logger = new EventLogLogger(loggerName, new EventLogSettings() { EventLog = testEventLog });

            // Act
            logger.LogInformation(message);

            // Assert
            Assert.Equal(expectedMessages.Length, testEventLog.Messages.Count);
            Assert.Equal(expectedMessages, testEventLog.Messages);
        }

        private class TestEventLog : IEventLog
        {
            public TestEventLog(int maxMessageSize)
            {
                MaxMessageSize = maxMessageSize;
                Messages = new List<string>();
            }

            public int MaxMessageSize { get; }

            public List<string> Messages { get; }

            public void WriteEntry(string message, EventLogEntryType type, int eventID, short category)
            {
                Messages.Add(message);
            }
        }
    }
}
#elif NETCOREAPP2_0
#else
#error Target framework needs to be updated
#endif