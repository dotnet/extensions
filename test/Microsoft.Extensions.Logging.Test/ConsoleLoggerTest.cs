// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Test.Console;
using Microsoft.Extensions.Primitives;
using System.Threading;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class ConsoleLoggerTest
    {
        private readonly string _paddingString;
        private const string _loggerName = "test";
        private const string _state = "This is a test, and {curly braces} are just fine!";
        private readonly Func<object, Exception, string> _theMessageAndError;

        private Tuple<ConsoleLogger, ConsoleSink> SetUp(Func<string, LogLevel, bool> filter, bool includeScopes = false)
        {
            // Arrange
            var sink = new ConsoleSink();
            var console = new TestConsole(sink);
            var logger = new ConsoleLogger(_loggerName, filter, includeScopes);
            logger.Console = console;
            return new Tuple<ConsoleLogger, ConsoleSink>(logger, sink);
        }

        public ConsoleLoggerTest()
        {
            var loglevelStringWithPadding = "INFO: ";
            _paddingString = new string(' ', loglevelStringWithPadding.Length);
            _theMessageAndError = ((message, error) => message + Environment.NewLine + _paddingString + error);
        }

        private Tuple<ILoggerFactory, ConsoleSink> SetUpFactory(Func<string, LogLevel, bool> filter)
        {
            var t = SetUp(null);
            var logger = t.Item1;
            var sink = t.Item2;

            var provider = new Mock<ILoggerProvider>();
            provider.Setup(f => f.CreateLogger(
                It.IsAny<string>()))
                .Returns(logger);

            var factory = new LoggerFactory();
            factory.AddProvider(provider.Object);

            return new Tuple<ILoggerFactory, ConsoleSink>(factory, sink);
        }

        [Fact]
        public void ThrowsException_WhenNoMessageAndExceptionAreProvided()
        {
            // Arrange
            var t = SetUp(null);
            var logger = (ILogger)t.Item1;
            var sink = t.Item2;
            var expectedExceptionMessage = "No message or exception details were found " +
                    "to create a message for the log.";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => logger.LogCritical(state: null));
            Assert.Equal(expectedExceptionMessage, exception.Message);
            exception = Assert.Throws<InvalidOperationException>(() => logger.LogCritical(state: null, error: null));
            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        [Fact]
        public void DoesNotLog_NewLine_WhenNoExceptionIsProvided()
        {
            // Arrange
            var t = SetUp(null);
            var logger = (ILogger)t.Item1;
            var sink = t.Item2;
            var logMessage = "Route with name 'Default' was not found.";
            var expectedMessage = _paddingString + logMessage + Environment.NewLine;

            // Act
            logger.LogCritical(logMessage);
            logger.LogCritical(logMessage, error: null);
            logger.LogCritical(eventId: 10, message: logMessage);
            logger.LogCritical(eventId: 10, message: logMessage, error: null);

            // Assert
            Assert.Equal(12, sink.Writes.Count);
            Assert.Equal(expectedMessage, sink.Writes[2].Message);
            Assert.Equal(expectedMessage, sink.Writes[5].Message);
            Assert.Equal(expectedMessage, sink.Writes[8].Message);
            Assert.Equal(expectedMessage, sink.Writes[11].Message);
        }

        [Theory]
        [InlineData("Route with name 'Default' was not found.")]
        [InlineData("")]
        public void Writes_NewLine_WhenExceptionIsProvided(string message)
        {
            // Arrange
            var t = SetUp(null);
            var logger = (ILogger)t.Item1;
            var sink = t.Item2;
            var eventId = 10;
            var exception = new InvalidOperationException("Invalid value");
            var expectedMessage =
                _paddingString + message + Environment.NewLine
                + _paddingString + ReplaceMessageNewLinesWithPadding(exception.ToString()) + Environment.NewLine;

            // Act
            logger.LogCritical(message, exception);
            logger.LogCritical(eventId, message, exception);

            // Assert
            Assert.Equal(6, sink.Writes.Count);
            Assert.Equal(expectedMessage, sink.Writes[2].Message);
            Assert.Equal(expectedMessage, sink.Writes[5].Message);
        }

        [Fact]
        public void WritesException_WhenNoMessageIsProvided()
        {
            // Arrange
            var t = SetUp(null);
            var logger = (ILogger)t.Item1;
            var sink = t.Item2;
            var exception = new InvalidOperationException("Invalid value");
            var expectedMessage =
                _paddingString + ReplaceMessageNewLinesWithPadding(exception.ToString()) + Environment.NewLine;

            // Act
            logger.LogCritical(state: null, error: exception);
            logger.LogCritical(10, state: null, error: exception);

            // Assert
            Assert.Equal(6, sink.Writes.Count);
            Assert.Equal(expectedMessage, sink.Writes[2].Message);
            Assert.Equal(expectedMessage, sink.Writes[5].Message);
        }

        [Fact]
        public void LogsWhenNullFilterGiven()
        {
            // Arrange
            var t = SetUp(null);
            var logger = t.Item1;
            var sink = t.Item2;

            // Act
            logger.Log(LogLevel.Information, 0, _state, null, null);

            // Assert
            Assert.Equal(3, sink.Writes.Count);
        }

        [Fact]
        public void CriticalFilter_LogsWhenAppropriate()
        {
            // Arrange
            var t = SetUp((category, logLevel) => logLevel >= LogLevel.Critical);
            var logger = t.Item1;
            var sink = t.Item2;

            // Act
            logger.Log(LogLevel.Warning, 0, _state, null, null);

            // Assert
            Assert.Equal(0, sink.Writes.Count);

            // Act
            logger.Log(LogLevel.Critical, 0, _state, null, null);

            // Assert
            Assert.Equal(3, sink.Writes.Count);
        }

        [Fact]
        public void ErrorFilter_LogsWhenAppropriate()
        {
            // Arrange
            var t = SetUp((category, logLevel) => logLevel >= LogLevel.Error);
            var logger = t.Item1;
            var sink = t.Item2;

            // Act
            logger.Log(LogLevel.Warning, 0, _state, null, null);

            // Assert
            Assert.Equal(0, sink.Writes.Count);

            // Act
            logger.Log(LogLevel.Error, 0, _state, null, null);

            // Assert
            Assert.Equal(3, sink.Writes.Count);
        }

        [Fact]
        public void WarningFilter_LogsWhenAppropriate()
        {
            // Arrange
            var t = SetUp((category, logLevel) => logLevel >= LogLevel.Warning);
            var logger = t.Item1;
            var sink = t.Item2;

            // Act
            logger.Log(LogLevel.Information, 0, _state, null, null);

            // Assert
            Assert.Equal(0, sink.Writes.Count);

            // Act
            logger.Log(LogLevel.Warning, 0, _state, null, null);

            // Assert
            Assert.Equal(3, sink.Writes.Count);
        }

        [Fact]
        public void InformationFilter_LogsWhenAppropriate()
        {
            // Arrange
            var t = SetUp((category, logLevel) => logLevel >= LogLevel.Information);
            var logger = t.Item1;
            var sink = t.Item2;

            // Act
            logger.Log(LogLevel.Verbose, 0, _state, null, null);

            // Assert
            Assert.Equal(0, sink.Writes.Count);

            // Act
            logger.Log(LogLevel.Information, 0, _state, null, null);

            // Assert
            Assert.Equal(3, sink.Writes.Count);
        }

        [Fact]
        public void VerboseFilter_LogsWhenAppropriate()
        {
            // Arrange
            var t = SetUp((category, logLevel) => logLevel >= LogLevel.Verbose);
            var logger = t.Item1;
            var sink = t.Item2;

            // Act
            logger.Log(LogLevel.Critical, 0, _state, null, null);
            logger.Log(LogLevel.Error, 0, _state, null, null);
            logger.Log(LogLevel.Warning, 0, _state, null, null);
            logger.Log(LogLevel.Information, 0, _state, null, null);
            logger.Log(LogLevel.Verbose, 0, _state, null, null);

            // Assert
            Assert.Equal(15, sink.Writes.Count);
        }

        [Fact]
        public void WriteCritical_LogsCorrectColors()
        {
            // Arrange
            var t = SetUp(null);
            var logger = t.Item1;
            var sink = t.Item2;

            // Act
            logger.Log(LogLevel.Critical, 0, _state, null, null);

            // Assert
            Assert.Equal(3, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(ConsoleColor.Red, write.BackgroundColor);
            Assert.Equal(ConsoleColor.White, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
            write = sink.Writes[2];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.White, write.ForegroundColor);
        }

        [Fact]
        public void WriteError_LogsCorrectColors()
        {
            // Arrange
            var t = SetUp(null);
            var logger = t.Item1;
            var sink = t.Item2;

            // Act
            logger.Log(LogLevel.Error, 0, _state, null, null);

            // Assert
            Assert.Equal(3, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Red, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
            write = sink.Writes[2];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.White, write.ForegroundColor);
        }

        [Fact]
        public void WriteWarning_LogsCorrectColors()
        {
            // Arrange
            var t = SetUp(null);
            var logger = t.Item1;
            var sink = t.Item2;

            // Act
            logger.Log(LogLevel.Warning, 0, _state, null, null);

            // Assert
            Assert.Equal(3, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.DarkYellow, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
            write = sink.Writes[2];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.White, write.ForegroundColor);
        }

        [Fact]
        public void WriteInformation_LogsCorrectColors()
        {
            // Arrange
            var t = SetUp(null);
            var logger = t.Item1;
            var sink = t.Item2;

            // Act
            logger.Log(LogLevel.Information, 0, _state, null, null);

            // Assert
            Assert.Equal(3, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.DarkGreen, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
            write = sink.Writes[2];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.White, write.ForegroundColor);
        }

        [Fact]
        public void WriteVerbose_LogsCorrectColors()
        {
            // Arrange
            var t = SetUp(null);
            var logger = t.Item1;
            var sink = t.Item2;

            // Act
            logger.Log(LogLevel.Verbose, 0, _state, null, null);

            // Assert
            Assert.Equal(3, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
            write = sink.Writes[2];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.White, write.ForegroundColor);
        }

        [Fact]
        public void WriteCore_LogsCorrectMessages()
        {
            // Arrange
            var t = SetUp(null);
            var logger = t.Item1;
            var sink = t.Item2;
            var ex = new Exception();


            // Act
            logger.Log(LogLevel.Critical, 0, _state, ex, _theMessageAndError);
            logger.Log(LogLevel.Error, 0, _state, ex, _theMessageAndError);
            logger.Log(LogLevel.Warning, 0, _state, ex, _theMessageAndError);
            logger.Log(LogLevel.Information, 0, _state, ex, _theMessageAndError);
            logger.Log(LogLevel.Verbose, 0, _state, ex, _theMessageAndError);
            logger.Log(LogLevel.Debug, 0, _state, ex, _theMessageAndError);

            // Assert
            Assert.Equal(18, sink.Writes.Count);
            Assert.Equal(GetMessage("crit", 0, ex), GetMessage(sink.Writes.GetRange(0, 3)));
            Assert.Equal(GetMessage("fail", 0, ex), GetMessage(sink.Writes.GetRange(3, 3)));
            Assert.Equal(GetMessage("warn", 0, ex), GetMessage(sink.Writes.GetRange(6, 3)));
            Assert.Equal(GetMessage("info", 0, ex), GetMessage(sink.Writes.GetRange(9, 3)));
            Assert.Equal(GetMessage("verb", 0, ex), GetMessage(sink.Writes.GetRange(12, 3)));
            Assert.Equal(GetMessage("dbug", 0, ex), GetMessage(sink.Writes.GetRange(15, 3)));
        }

        [Fact]
        public void NoLogScope_DoesNotWriteAnyScopeContentToOutput()
        {
            // Arrange
            var t = SetUp(filter: null, includeScopes: true);
            var logger = t.Item1;
            var sink = t.Item2;

            // Act
            logger.Log(LogLevel.Warning, 0, _state, null, null);

            // Assert
            Assert.Equal(3, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.DarkYellow, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
            write = sink.Writes[2];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.White, write.ForegroundColor);
        }

        [Fact]
        public void WritingScopes_LogsWithCorrectColors()
        {
            // Arrange
            var t = SetUp(filter: null, includeScopes: true);
            var logger = t.Item1;
            var sink = t.Item2;
            var id = Guid.NewGuid();
            var scopeMessage = "RequestId: {RequestId}";

            // Act
            using (logger.BeginScope(scopeMessage, id))
            {
                logger.Log(LogLevel.Information, 0, _state, null, null);
            }

            // Assert
            Assert.Equal(4, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.DarkGreen, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
            write = sink.Writes[2];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
            write = sink.Writes[3];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.White, write.ForegroundColor);
        }

        [Fact]
        public void WritingScopes_LogsExpectedMessage()
        {
            // Arrange
            var t = SetUp(filter: null, includeScopes: true);
            var logger = t.Item1;
            var sink = t.Item2;
            var expectedMessage =
                _paddingString
                + $"=> RequestId: 100"
                + Environment.NewLine;

            // Act
            using (logger.BeginScope("RequestId: {RequestId}", 100))
            {
                logger.Log(LogLevel.Information, 0, _state, null, null);
            }

            // Assert
            Assert.Equal(4, sink.Writes.Count);
            // scope
            var write = sink.Writes[2];
            Assert.Equal(expectedMessage, write.Message);
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
        }

        [Fact]
        public void WritingNestedScopes_LogsExpectedMessage()
        {
            // Arrange
            var t = SetUp(filter: null, includeScopes: true);
            var logger = t.Item1;
            var sink = t.Item2;
            var expectedMessage =
                _paddingString
                + $"=> RequestId: 100 => Request matched action: Index"
                + Environment.NewLine;

            // Act
            using (logger.BeginScope("RequestId: {RequestId}", 100))
            {
                using (logger.BeginScope("Request matched action: {ActionName}", "Index"))
                {
                    logger.Log(LogLevel.Information, 0, _state, null, null);
                }
            }

            // Assert
            Assert.Equal(4, sink.Writes.Count);
            // scope
            var write = sink.Writes[2];
            Assert.Equal(expectedMessage, write.Message);
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
        }

        [Fact]
        public void WritingMultipleScopes_LogsExpectedMessage()
        {
            // Arrange
            var t = SetUp(filter: null, includeScopes: true);
            var logger = t.Item1;
            var sink = t.Item2;
            var expectedMessage1 =
                _paddingString
                + $"=> RequestId: 100 => Request matched action: Index"
                + Environment.NewLine;
            var expectedMessage2 =
                _paddingString
                + $"=> RequestId: 100 => Created product: Car"
                + Environment.NewLine;

            // Act
            using (logger.BeginScope("RequestId: {RequestId}", 100))
            {
                using (logger.BeginScope("Request matched action: {ActionName}", "Index"))
                {
                    logger.Log(LogLevel.Information, 0, _state, null, null);
                }

                using (logger.BeginScope("Created product: {ProductName}", "Car"))
                {
                    logger.Log(LogLevel.Information, 0, _state, null, null);
                }
            }

            // Assert
            Assert.Equal(8, sink.Writes.Count);
            // scope
            var write = sink.Writes[2];
            Assert.Equal(expectedMessage1, write.Message);
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
            write = sink.Writes[6];
            Assert.Equal(expectedMessage2, write.Message);
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
        }

        [Fact]
        public void CallingBeginScopeOnLogger_AlwaysReturnsNewDisposableInstance()
        {
            // Arrange
            var t = SetUp(null);
            var logger = t.Item1;
            var sink = t.Item2;

            // Act
            var disposable1 = logger.BeginScopeImpl("Scope1");
            var disposable2 = logger.BeginScopeImpl("Scope2");

            // Assert
            Assert.NotNull(disposable1);
            Assert.NotNull(disposable2);
            Assert.NotSame(disposable1, disposable2);
        }

        [Fact]
        public void CallingBeginScopeOnLogger_ReturnsNonNullableInstance()
        {
            // Arrange
            var t = SetUp(null);
            var logger = t.Item1;
            var sink = t.Item2;

            // Act
            var disposable = logger.BeginScopeImpl("Scope1");

            // Assert
            Assert.NotNull(disposable);
        }

        [Fact]
        public void ConsoleLogger_ReloadSettings_CanChangeLogLevel()
        {
            // Arrange
            var settings = new MockConsoleLoggerSettings()
            {
                Cancel = new CancellationTokenSource(),
                Switches =
                {
                    ["Test"] = LogLevel.Information,
                }
            };

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole(settings);

            var logger = loggerFactory.CreateLogger("Test");
            Assert.False(logger.IsEnabled(LogLevel.Verbose));

            settings.Switches["Test"] = LogLevel.Verbose;

            var cancellationTokenSource = settings.Cancel;
            settings.Cancel = new CancellationTokenSource();

            // Act
            cancellationTokenSource.Cancel();

            // Assert
            Assert.True(logger.IsEnabled(LogLevel.Verbose));
        }

        [Fact]
        public void ConsoleLogger_ReloadSettings_CanReloadMultipleTimes()
        {
            // Arrange
            var settings = new MockConsoleLoggerSettings()
            {
                Cancel = new CancellationTokenSource(),
                Switches =
                {
                    ["Test"] = LogLevel.Information,
                }
            };

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole(settings);

            var logger = loggerFactory.CreateLogger("Test");
            Assert.False(logger.IsEnabled(LogLevel.Verbose));

            // Act & Assert
            for (var i = 0; i < 10; i++)
            {
                settings.Switches["Test"] = i % 2 == 0 ? LogLevel.Information : LogLevel.Verbose;

                var cancellationTokenSource = settings.Cancel;
                settings.Cancel = new CancellationTokenSource();

                cancellationTokenSource.Cancel();

                Assert.Equal(i % 2 == 1, logger.IsEnabled(LogLevel.Verbose));
            }
        }

        private string GetMessage(string logLevelString, int eventId, Exception exception)
        {
            var loglevelStringWithPadding = $"{logLevelString}: ";

            return
                loglevelStringWithPadding + $"{_loggerName}[{eventId}]" + Environment.NewLine
                + _paddingString + ReplaceMessageNewLinesWithPadding(_theMessageAndError(_state, exception)) + Environment.NewLine;
        }

        private string ReplaceMessageNewLinesWithPadding(string message)
        {
            return message.Replace(Environment.NewLine, Environment.NewLine + _paddingString);
        }

        private string GetMessage(List<ConsoleContext> contexts)
        {
            return string.Join("", contexts.Select(c => c.Message));
        }

        private class MockConsoleLoggerSettings : IConsoleLoggerSettings
        {
            public CancellationTokenSource Cancel { get; set; }

            public IChangeToken ChangeToken => new CancellationChangeToken(Cancel.Token);

            public IDictionary<string, LogLevel> Switches { get; } = new Dictionary<string, LogLevel>();

            public bool IncludeScopes { get; set; }

            public IConsoleLoggerSettings Reload()
            {
                return this;
            }

            public bool TryGetSwitch(string name, out LogLevel level)
            {
                return Switches.TryGetValue(name, out level);
            }
        }
    }
}