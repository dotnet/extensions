// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Test.Console;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Extensions.Logging.Test
{
    public class ConsoleLoggerTest
    {
        private const string _name = "test";
        private const string _state = "This is a test, and {curly braces} are just fine!";

        private static readonly Func<object, Exception, string> TheMessageAndError =
            (message, error) => string.Format(CultureInfo.CurrentCulture, "{0}\r\n{1}", message, error);

        private Tuple<ConsoleLogger, ConsoleSink> SetUp(Func<string, LogLevel, bool> filter)
        {
            // Arrange
            var sink = new ConsoleSink();
            var console = new TestConsole(sink);
            var logger = new ConsoleLogger(_name, filter);
            logger.Console = console;
            return new Tuple<ConsoleLogger, ConsoleSink>(logger, sink);
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
        public void MessagesAreNotLoggedWhenBelowMinimumLevel()
        {
            // arrange
            var t = SetUpFactory(null);
            var factory = t.Item1;
            var sink = t.Item2;
            var logger = factory.CreateLogger(_name);


            // act
            logger.Log(LogLevel.Debug, 0, _state, null, null);
            logger.Log(LogLevel.Verbose, 0, _state, null, null);

            // assert
            Assert.Equal(LogLevel.Verbose, factory.MinimumLevel);
            Assert.Equal(1, sink.Writes.Count);
        }

        [Theory]
        [InlineData(LogLevel.Debug, 6, true, true)]
        [InlineData(LogLevel.Verbose, 5, false, true)]
        [InlineData(LogLevel.Information, 4, false, true)]
        [InlineData(LogLevel.Warning, 3, false, false)]
        [InlineData(LogLevel.Error, 2, false, false)]
        [InlineData(LogLevel.Critical, 1, false, false)]
        public void MinimumLogLevelCanBeChanged(LogLevel minimumLevel, int expectedMessageCount, bool enabledDebug, bool enabledInformation)
        {
            var t = SetUpFactory(null);
            var factory = t.Item1;
            var sink = t.Item2;
            var logger = factory.CreateLogger(_name);

            factory.MinimumLevel = minimumLevel;

            // act
            logger.Log(LogLevel.Debug, 0, _state, null, null);
            logger.Log(LogLevel.Verbose, 0, _state, null, null);
            logger.Log(LogLevel.Information, 0, _state, null, null);
            logger.Log(LogLevel.Warning, 0, _state, null, null);
            logger.Log(LogLevel.Error, 0, _state, null, null);
            logger.Log(LogLevel.Critical, 0, _state, null, null);

            // assert
            Assert.Equal(minimumLevel, factory.MinimumLevel);
            Assert.Equal(expectedMessageCount, sink.Writes.Count);
            Assert.Equal(enabledDebug, logger.IsEnabled(LogLevel.Debug));
            Assert.Equal(enabledInformation, logger.IsEnabled(LogLevel.Information));
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
            var expectedMessage = "Route with name 'Default' was not found.";

            // Act
            logger.LogCritical(expectedMessage);
            logger.LogCritical(expectedMessage, error: null);
            logger.LogCritical(eventId: 10, message: expectedMessage);
            logger.LogCritical(eventId: 10, message: expectedMessage, error: null);

            // Assert
            Assert.Equal(4, sink.Writes.Count);
            Assert.Equal($"critical: [{_name}] {expectedMessage}", sink.Writes[0].Message);
            Assert.Equal($"critical: [{_name}] {expectedMessage}", sink.Writes[1].Message);
            Assert.Equal($"critical: [{_name}] {expectedMessage}", sink.Writes[2].Message);
            Assert.Equal($"critical: [{_name}] {expectedMessage}", sink.Writes[3].Message);
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
            var exception = new InvalidOperationException("Invalid value");
            var expectedMessage = $"{message}{Environment.NewLine}{exception}";

            // Act
            logger.LogCritical(message, exception);
            logger.LogCritical(10, message, exception);

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            Assert.Equal($"critical: [{_name}] {expectedMessage}", sink.Writes[0].Message);
            Assert.Equal($"critical: [{_name}] {expectedMessage}", sink.Writes[1].Message);
        }

        [Fact]
        public void WritesException_WhenNoMessageIsProvided()
        {
            // Arrange
            var t = SetUp(null);
            var logger = (ILogger)t.Item1;
            var sink = t.Item2;
            var exception = new InvalidOperationException("Invalid value");
            var expectedMessage = exception.ToString();

            // Act
            logger.LogCritical(state: null, error: exception);
            logger.LogCritical(10, state: null, error: exception);

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            Assert.Equal($"critical: [{_name}] {expectedMessage}", sink.Writes[0].Message);
            Assert.Equal($"critical: [{_name}] {expectedMessage}", sink.Writes[1].Message);
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
            Assert.Equal(1, sink.Writes.Count);
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
            Assert.Equal(1, sink.Writes.Count);
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
            Assert.Equal(1, sink.Writes.Count);
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
            Assert.Equal(1, sink.Writes.Count);
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
            Assert.Equal(1, sink.Writes.Count);
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
            Assert.Equal(5, sink.Writes.Count);
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
            Assert.Equal(1, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(ConsoleColor.Red, write.BackgroundColor);
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
            Assert.Equal(1, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Red, write.ForegroundColor);
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
            Assert.Equal(1, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Yellow, write.ForegroundColor);
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
            Assert.Equal(1, sink.Writes.Count);
            var write = sink.Writes[0];
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
            Assert.Equal(1, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
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
            logger.Log(LogLevel.Critical, 0, _state, ex, TheMessageAndError);
            logger.Log(LogLevel.Error, 0, _state, ex, TheMessageAndError);
            logger.Log(LogLevel.Warning, 0, _state, ex, TheMessageAndError);
            logger.Log(LogLevel.Information, 0, _state, ex, TheMessageAndError);
            logger.Log(LogLevel.Verbose, 0, _state, ex, TheMessageAndError);
            logger.Log(LogLevel.Debug, 0, _state, ex, TheMessageAndError);

            // Assert
            Assert.Equal(6, sink.Writes.Count);
            Assert.Equal(getMessage("critical", ex), sink.Writes[0].Message);
            Assert.Equal(getMessage("error   ", ex), sink.Writes[1].Message);
            Assert.Equal(getMessage("warning ", ex), sink.Writes[2].Message);
            Assert.Equal(getMessage("info    ", ex), sink.Writes[3].Message);
            Assert.Equal(getMessage("verbose ", ex), sink.Writes[4].Message);
            Assert.Equal(getMessage("debug   ", ex), sink.Writes[5].Message);
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

        private string getMessage(string logLevelString, Exception exception)
        {
            return $"{logLevelString}: [{_name}] {TheMessageAndError(_state, exception)}";

        }
    }
}