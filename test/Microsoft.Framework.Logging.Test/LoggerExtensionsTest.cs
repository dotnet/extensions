// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Framework.Logging.Test
{
    public class LoggerExtensionsTest
    {
        private const string _name = "test";
        private const string _state = "testing";
        private const string _format = "{0}, {1}";
        private static Exception _exception = new InvalidOperationException();

        private TestLogger SetUp(TestSink sink)
        {
            // Arrange
            var logger = new TestLogger(_name, sink, enabled: true);
            return logger;
        }

        [Fact]
        public void MessageOnly_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.WriteVerbose(_state);
            logger.WriteInformation(_state);
            logger.WriteWarning(_state);
            logger.WriteError(_state);
            logger.WriteCritical(_state);
            logger.WriteDebug(_state);

            // Assert
            Assert.Equal(6, sink.Writes.Count);

            var verbose = sink.Writes[0];
            Assert.Equal(LogLevel.Verbose, verbose.LogLevel);
            Assert.Equal(_state, verbose.State);
            Assert.Equal(0, verbose.EventId);
            Assert.Equal(null, verbose.Exception);

            var information = sink.Writes[1];
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(_state, information.State);
            Assert.Equal(0, information.EventId);
            Assert.Equal(null, information.Exception);

            var warning = sink.Writes[2];
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(_state, warning.State);
            Assert.Equal(0, warning.EventId);
            Assert.Equal(null, warning.Exception);

            var error = sink.Writes[3];
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(_state, error.State);
            Assert.Equal(0, error.EventId);
            Assert.Equal(null, error.Exception);

            var critical = sink.Writes[4];
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(_state, critical.State);
            Assert.Equal(0, critical.EventId);
            Assert.Equal(null, critical.Exception);

            var debug = sink.Writes[5];
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(_state, debug.State);
            Assert.Equal(0, debug.EventId);
            Assert.Equal(null, debug.Exception);
        }

        [Fact]
        public void FormatMessage_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.WriteVerbose(_format, "test1", "test2");
            logger.WriteInformation(_format, "test1", "test2");
            logger.WriteWarning(_format, "test1", "test2");
            logger.WriteError(_format, "test1", "test2");
            logger.WriteCritical(_format, "test1", "test2");
            logger.WriteDebug(_format, "test1", "test2");

            // Assert
            Assert.Equal(6, sink.Writes.Count);

            var verbose = sink.Writes[0];
            Assert.Equal(LogLevel.Verbose, verbose.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), ((ILoggerStructure)verbose.State).Format());
            Assert.Equal(0, verbose.EventId);
            Assert.Equal(null, verbose.Exception);

            var information = sink.Writes[1];
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), ((ILoggerStructure)information.State).Format());
            Assert.Equal(0, information.EventId);
            Assert.Equal(null, information.Exception);

            var warning = sink.Writes[2];
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), ((ILoggerStructure)warning.State).Format());
            Assert.Equal(0, warning.EventId);
            Assert.Equal(null, warning.Exception);

            var error = sink.Writes[3];
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), ((ILoggerStructure)error.State).Format());
            Assert.Equal(0, error.EventId);
            Assert.Equal(null, error.Exception);

            var critical = sink.Writes[4];
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), ((ILoggerStructure)critical.State).Format());
            Assert.Equal(0, critical.EventId);
            Assert.Equal(null, critical.Exception);

            var debug = sink.Writes[5];
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), ((ILoggerStructure)debug.State).Format());
            Assert.Equal(0, debug.EventId);
            Assert.Equal(null, debug.Exception);
        }

        [Fact]
        public void MessageAndEventId_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.WriteVerbose(1, _state);
            logger.WriteInformation(2, _state);
            logger.WriteWarning(3, _state);
            logger.WriteError(4, _state);
            logger.WriteCritical(5, _state);
            logger.WriteDebug(6, _state);

            // Assert
            Assert.Equal(6, sink.Writes.Count);

            var verbose = sink.Writes[0];
            Assert.Equal(LogLevel.Verbose, verbose.LogLevel);
            Assert.Equal(_state, verbose.State);
            Assert.Equal(1, verbose.EventId);
            Assert.Equal(null, verbose.Exception);

            var information = sink.Writes[1];
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(_state, information.State);
            Assert.Equal(2, information.EventId);
            Assert.Equal(null, information.Exception);

            var warning = sink.Writes[2];
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(_state, warning.State);
            Assert.Equal(3, warning.EventId);
            Assert.Equal(null, warning.Exception);

            var error = sink.Writes[3];
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(_state, error.State);
            Assert.Equal(4, error.EventId);
            Assert.Equal(null, error.Exception);

            var critical = sink.Writes[4];
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(_state, critical.State);
            Assert.Equal(5, critical.EventId);
            Assert.Equal(null, critical.Exception);

            var debug = sink.Writes[5];
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(_state, debug.State);
            Assert.Equal(6, debug.EventId);
            Assert.Equal(null, debug.Exception);
        }

        [Fact]
        public void FormatMessageAndEventId_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.WriteVerbose(1, _format, "test1", "test2");
            logger.WriteInformation(2, _format, "test1", "test2");
            logger.WriteWarning(3, _format, "test1", "test2");
            logger.WriteError(4, _format, "test1", "test2");
            logger.WriteCritical(5, _format, "test1", "test2");
            logger.WriteDebug(6, _format, "test1", "test2");

            // Assert
            Assert.Equal(6, sink.Writes.Count);

            var verbose = sink.Writes[0];
            Assert.Equal(LogLevel.Verbose, verbose.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), ((ILoggerStructure)verbose.State).Format());
            Assert.Equal(1, verbose.EventId);
            Assert.Equal(null, verbose.Exception);

            var information = sink.Writes[1];
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), ((ILoggerStructure)information.State).Format());
            Assert.Equal(2, information.EventId);
            Assert.Equal(null, information.Exception);

            var warning = sink.Writes[2];
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), ((ILoggerStructure)warning.State).Format());
            Assert.Equal(3, warning.EventId);
            Assert.Equal(null, warning.Exception);

            var error = sink.Writes[3];
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), ((ILoggerStructure)error.State).Format());
            Assert.Equal(4, error.EventId);
            Assert.Equal(null, error.Exception);

            var critical = sink.Writes[4];
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), ((ILoggerStructure)critical.State).Format());
            Assert.Equal(5, critical.EventId);
            Assert.Equal(null, critical.Exception);

            var debug = sink.Writes[5];
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), ((ILoggerStructure)debug.State).Format());
            Assert.Equal(6, debug.EventId);
            Assert.Equal(null, debug.Exception);
        }

        [Fact]
        public void MessageAndError_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.WriteWarning(_state, _exception);
            logger.WriteError(_state, _exception);
            logger.WriteCritical(_state, _exception);

            // Assert
            Assert.Equal(3, sink.Writes.Count);

            var warning = sink.Writes[0];
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(_state, warning.State);
            Assert.Equal(0, warning.EventId);
            Assert.Equal(_exception, warning.Exception);

            var error = sink.Writes[1];
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(_state, error.State);
            Assert.Equal(0, error.EventId);
            Assert.Equal(_exception, error.Exception);

            var critical = sink.Writes[2];
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(_state, critical.State);
            Assert.Equal(0, critical.EventId);
            Assert.Equal(_exception, critical.Exception);
        }

        [Fact]
        public void MessageEventIdAndError_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.WriteWarning(3, _state, _exception);
            logger.WriteError(4, _state, _exception);
            logger.WriteCritical(5, _state, _exception);

            // Assert
            Assert.Equal(3, sink.Writes.Count);

            var warning = sink.Writes[0];
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(_state, warning.State);
            Assert.Equal(3, warning.EventId);
            Assert.Equal(_exception, warning.Exception);

            var error = sink.Writes[1];
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(_state, error.State);
            Assert.Equal(4, error.EventId);
            Assert.Equal(_exception, error.Exception);

            var critical = sink.Writes[2];
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(_state, critical.State);
            Assert.Equal(5, critical.EventId);
            Assert.Equal(_exception, critical.Exception);
        }

        [Fact]
        public void LoggerStructure_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);
            var testStructure = new TestStructure()
            {
                Message = "Test",
                Value = 1
            };

            // Act
            logger.WriteVerbose(testStructure);
            logger.WriteInformation(testStructure);
            logger.WriteWarning(testStructure);
            logger.WriteError(testStructure);
            logger.WriteCritical(testStructure);
            logger.WriteDebug(testStructure);

            // Assert
            Assert.Equal(6, sink.Writes.Count);

            var verbose = sink.Writes[0];
            Assert.Equal(LogLevel.Verbose, verbose.LogLevel);
            Assert.Equal(testStructure, verbose.State);
            Assert.Equal(0, verbose.EventId);
            Assert.Equal(null, verbose.Exception);
            Assert.Equal("Test 1", verbose.Formatter(verbose.State, verbose.Exception));

            var information = sink.Writes[1];
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(testStructure, information.State);
            Assert.Equal(0, information.EventId);
            Assert.Equal(null, information.Exception);
            Assert.Equal("Test 1", information.Formatter(information.State, information.Exception));

            var warning = sink.Writes[2];
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(testStructure, warning.State);
            Assert.Equal(0, warning.EventId);
            Assert.Equal(null, warning.Exception);
            Assert.Equal("Test 1", warning.Formatter(warning.State, warning.Exception));

            var error = sink.Writes[3];
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(testStructure, error.State);
            Assert.Equal(0, error.EventId);
            Assert.Equal(null, error.Exception);
            Assert.Equal("Test 1", error.Formatter(error.State, error.Exception));

            var critical = sink.Writes[4];
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(testStructure, critical.State);
            Assert.Equal(0, critical.EventId);
            Assert.Equal(null, critical.Exception);
            Assert.Equal("Test 1", critical.Formatter(critical.State, critical.Exception));

            var debug = sink.Writes[5];
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(testStructure, debug.State);
            Assert.Equal(0, debug.EventId);
            Assert.Equal(null, debug.Exception);
            Assert.Equal("Test 1", debug.Formatter(debug.State, debug.Exception));
        }

        [Fact]
        public void LoggerStructureAndEventId_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);
            var testStructure = new TestStructure()
            {
                Message = "Test",
                Value = 1
            };

            // Act
            logger.WriteVerbose(1, testStructure);
            logger.WriteInformation(2, testStructure);
            logger.WriteWarning(3, testStructure);
            logger.WriteError(4, testStructure);
            logger.WriteCritical(5, testStructure);
            logger.WriteDebug(6, testStructure);

            // Assert
            Assert.Equal(6, sink.Writes.Count);

            var verbose = sink.Writes[0];
            Assert.Equal(LogLevel.Verbose, verbose.LogLevel);
            Assert.Equal(testStructure, verbose.State);
            Assert.Equal(1, verbose.EventId);
            Assert.Equal(null, verbose.Exception);
            Assert.Equal("Test 1", verbose.Formatter(verbose.State, verbose.Exception));

            var information = sink.Writes[1];
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(testStructure, information.State);
            Assert.Equal(2, information.EventId);
            Assert.Equal(null, information.Exception);
            Assert.Equal("Test 1", information.Formatter(information.State, information.Exception));

            var warning = sink.Writes[2];
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(testStructure, warning.State);
            Assert.Equal(3, warning.EventId);
            Assert.Equal(null, warning.Exception);
            Assert.Equal("Test 1", warning.Formatter(warning.State, warning.Exception));

            var error = sink.Writes[3];
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(testStructure, error.State);
            Assert.Equal(4, error.EventId);
            Assert.Equal(null, error.Exception);
            Assert.Equal("Test 1", error.Formatter(error.State, error.Exception));

            var critical = sink.Writes[4];
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(testStructure, critical.State);
            Assert.Equal(5, critical.EventId);
            Assert.Equal(null, critical.Exception);
            Assert.Equal("Test 1", critical.Formatter(critical.State, critical.Exception));

            var debug = sink.Writes[5];
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(testStructure, debug.State);
            Assert.Equal(6, debug.EventId);
            Assert.Equal(null, debug.Exception);
            Assert.Equal("Test 1", debug.Formatter(debug.State, debug.Exception));
        }

        [Fact]
        public void LoggerStructureAndError_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);
            var testStructure = new TestStructure()
            {
                Message = "Test",
                Value = 1
            };

            // Act
            logger.WriteVerbose(testStructure, _exception);
            logger.WriteInformation(testStructure, _exception);
            logger.WriteWarning(testStructure, _exception);
            logger.WriteError(testStructure, _exception);
            logger.WriteCritical(testStructure, _exception);
            logger.WriteDebug(testStructure, _exception);

            // Assert
            Assert.Equal(6, sink.Writes.Count);

            var verbose = sink.Writes[0];
            Assert.Equal(LogLevel.Verbose, verbose.LogLevel);
            Assert.Equal(testStructure, verbose.State);
            Assert.Equal(0, verbose.EventId);
            Assert.Equal(_exception, verbose.Exception);
            Assert.Equal(
                "Test 1" + Environment.NewLine + _exception,
                verbose.Formatter(verbose.State, verbose.Exception));

            var information = sink.Writes[1];
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(testStructure, information.State);
            Assert.Equal(0, information.EventId);
            Assert.Equal(_exception, information.Exception);
            Assert.Equal(
                "Test 1" + Environment.NewLine + _exception,
                information.Formatter(information.State, information.Exception));

            var warning = sink.Writes[2];
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(testStructure, warning.State);
            Assert.Equal(0, warning.EventId);
            Assert.Equal(_exception, warning.Exception);
            Assert.Equal(
                "Test 1" + Environment.NewLine + _exception,
                warning.Formatter(warning.State, warning.Exception));

            var error = sink.Writes[3];
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(testStructure, error.State);
            Assert.Equal(0, error.EventId);
            Assert.Equal(_exception, error.Exception);
            Assert.Equal(
                "Test 1" + Environment.NewLine + _exception,
                error.Formatter(error.State, error.Exception));

            var critical = sink.Writes[4];
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(testStructure, critical.State);
            Assert.Equal(0, critical.EventId);
            Assert.Equal(_exception, critical.Exception);
            Assert.Equal(
                "Test 1" + Environment.NewLine + _exception,
                critical.Formatter(critical.State, critical.Exception));

            var debug = sink.Writes[5];
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(testStructure, debug.State);
            Assert.Equal(0, debug.EventId);
            Assert.Equal(_exception, debug.Exception);
            Assert.Equal(
                "Test 1" + Environment.NewLine + _exception,
                debug.Formatter(debug.State, debug.Exception));
        }

        public void LoggerStructureEventIdAndError_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);
            var testStructure = new TestStructure()
            {
                Message = "Test",
                Value = 1
            };

            // Act
            logger.WriteVerbose(1, testStructure, _exception);
            logger.WriteInformation(2, testStructure, _exception);
            logger.WriteWarning(3, testStructure, _exception);
            logger.WriteError(4, testStructure, _exception);
            logger.WriteCritical(5, testStructure, _exception);
            logger.WriteDebug(6, testStructure, _exception);

            // Assert
            Assert.Equal(6, sink.Writes.Count);

            var verbose = sink.Writes[0];
            Assert.Equal(LogLevel.Verbose, verbose.LogLevel);
            Assert.Equal(testStructure, verbose.State);
            Assert.Equal(1, verbose.EventId);
            Assert.Equal(_exception, verbose.Exception);
            Assert.Equal(
                "Test 1" + Environment.NewLine + _exception,
                verbose.Formatter(verbose.State, verbose.Exception));

            var information = sink.Writes[1];
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(testStructure, information.State);
            Assert.Equal(2, information.EventId);
            Assert.Equal(_exception, information.Exception);
            Assert.Equal(
                "Test 1" + Environment.NewLine + _exception,
                information.Formatter(information.State, information.Exception));

            var warning = sink.Writes[2];
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(testStructure, warning.State);
            Assert.Equal(3, warning.EventId);
            Assert.Equal(_exception, warning.Exception);
            Assert.Equal(
                "Test 1" + Environment.NewLine + _exception,
                warning.Formatter(warning.State, warning.Exception));

            var error = sink.Writes[3];
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(testStructure, error.State);
            Assert.Equal(4, error.EventId);
            Assert.Equal(_exception, error.Exception);
            Assert.Equal(
                "Test 1" + Environment.NewLine + _exception,
                error.Formatter(error.State, error.Exception));

            var critical = sink.Writes[4];
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(testStructure, critical.State);
            Assert.Equal(5, critical.EventId);
            Assert.Equal(_exception, critical.Exception);
            Assert.Equal(
                "Test 1" + Environment.NewLine + _exception,
                critical.Formatter(critical.State, critical.Exception));

            var debug = sink.Writes[4];
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(testStructure, debug.State);
            Assert.Equal(6, debug.EventId);
            Assert.Equal(_exception, debug.Exception);
            Assert.Equal(
                "Test 1" + Environment.NewLine + _exception,
                debug.Formatter(debug.State, debug.Exception));
        }

        private class TestStructure : LoggerStructureBase
        {
            public int Value { get; set; }

            public override string Format()
            {
                return Message + " " + Value;
            }
        }
    }
}