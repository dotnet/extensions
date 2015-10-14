// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Test;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Extensions.Logging
{
    public class LoggerMessageTest
    {
        [Fact]
        public void LogMessage()
        {
            // Arrange
            var controller = "home";
            var action = "index";
            var testSink = new TestSink();
            var testLogger = new TestLogger("testlogger", testSink, enabled: true);

            // Act
            testLogger.ActionMatched(controller, action);

            // Assert
            Assert.Equal(1, testSink.Writes.Count);
            var writeContext = testSink.Writes.First();
            var actualLogValues = Assert.IsAssignableFrom<ILogValues>(writeContext.State);
            AssertLogValues(
                new[] {
                    new KeyValuePair<string, object>("{OriginalFormat}", TestLoggerExtensions.ActionMatchedInfo.NamedStringFormat),
                    new KeyValuePair<string, object>("controller", controller),
                    new KeyValuePair<string, object>("action", action)
                },
                actualLogValues.GetValues());
            Assert.Equal(LogLevel.Information, writeContext.LogLevel);
            Assert.Equal(1, writeContext.EventId);
            Assert.Null(writeContext.Exception);
            Assert.Equal(
                string.Format(
                    TestLoggerExtensions.ActionMatchedInfo.FormatString,
                    controller,
                    action),
                actualLogValues.ToString());
        }

        [Fact]
        public void LogScope_WithoutAnyParameters()
        {
            // Arrange
            var testSink = new TestSink();
            var testLogger = new TestLogger("testlogger", testSink, enabled: true);

            // Act
            var disposable = testLogger.ScopeWithoutAnyParams();

            // Assert
            Assert.NotNull(disposable);
            Assert.Equal(0, testSink.Writes.Count);
            Assert.Equal(1, testSink.Scopes.Count);
            var scopeContext = testSink.Scopes.First();
            var actualLogValues = Assert.IsAssignableFrom<ILogValues>(scopeContext.Scope);
            AssertLogValues(new[]
            {
                new KeyValuePair<string, object>("{OriginalFormat}", TestLoggerExtensions.ScopeWithoutAnyParameters.Message)
            },
            actualLogValues.GetValues());
            Assert.Equal(
                TestLoggerExtensions.ScopeWithoutAnyParameters.Message,
                actualLogValues.ToString());
        }

        [Fact]
        public void LogScope_WithOneParameter()
        {
            // Arrange
            var param1 = Guid.NewGuid().ToString();
            var testSink = new TestSink();
            var testLogger = new TestLogger("testlogger", testSink, enabled: true);

            // Act
            var disposable = testLogger.ScopeWithOneParam(param1);

            // Assert
            Assert.NotNull(disposable);
            Assert.Equal(0, testSink.Writes.Count);
            Assert.Equal(1, testSink.Scopes.Count);
            var scopeContext = testSink.Scopes.First();
            var actualLogValues = Assert.IsAssignableFrom<ILogValues>(scopeContext.Scope);
            AssertLogValues(new[]
            {
                new KeyValuePair<string, object>("RequestId", param1),
                new KeyValuePair<string, object>("{OriginalFormat}", TestLoggerExtensions.ScopeWithOneParameter.NamedStringFormat)
            },
            actualLogValues.GetValues());
            Assert.Equal(
                string.Format(TestLoggerExtensions.ScopeWithOneParameter.FormatString, param1),
                actualLogValues.ToString());
        }

        [Fact]
        public void LogScope_WithTwoParameters()
        {
            // Arrange
            var param1 = "foo";
            var param2 = "bar";
            var testSink = new TestSink();
            var testLogger = new TestLogger("testlogger", testSink, enabled: true);

            // Act
            var disposable = testLogger.ScopeWithTwoParams(param1, param2);

            // Assert
            Assert.NotNull(disposable);
            Assert.Equal(0, testSink.Writes.Count);
            Assert.Equal(1, testSink.Scopes.Count);
            var scopeContext = testSink.Scopes.First();
            var actualLogValues = Assert.IsAssignableFrom<ILogValues>(scopeContext.Scope);
            AssertLogValues(new[]
            {
                new KeyValuePair<string, object>("param1", param1),
                new KeyValuePair<string, object>("param2", param2),
                new KeyValuePair<string, object>("{OriginalFormat}", TestLoggerExtensions.ScopeInfoWithTwoParameters.NamedStringFormat)
            },
            actualLogValues.GetValues());
            Assert.Equal(
                string.Format(TestLoggerExtensions.ScopeInfoWithTwoParameters.FormatString, param1, param2),
                actualLogValues.ToString());
        }

        [Fact]
        public void LogScope_WithThreeParameters()
        {
            // Arrange
            var param1 = "foo";
            var param2 = "bar";
            int param3 = 10;
            var testSink = new TestSink();
            var testLogger = new TestLogger("testlogger", testSink, enabled: true);

            // Act
            var disposable = testLogger.ScopeWithThreeParams(param1, param2, param3);

            // Assert
            Assert.NotNull(disposable);
            Assert.Equal(0, testSink.Writes.Count);
            Assert.Equal(1, testSink.Scopes.Count);
            var scopeContext = testSink.Scopes.First();
            var actualLogValues = Assert.IsAssignableFrom<ILogValues>(scopeContext.Scope);
            AssertLogValues(new[]
            {
                new KeyValuePair<string, object>("param1", param1),
                new KeyValuePair<string, object>("param2", param2),
                new KeyValuePair<string, object>("param3", param3),
                new KeyValuePair<string, object>("{OriginalFormat}", TestLoggerExtensions.ScopeInfoWithThreeParameters.NamedStringFormat)
            },
            actualLogValues.GetValues());
            Assert.Equal(
                string.Format(TestLoggerExtensions.ScopeInfoWithThreeParameters.FormatString, param1, param2, param3),
                actualLogValues.ToString());
        }

        private void AssertLogValues(
            IEnumerable<KeyValuePair<string, object>> expected,
            IEnumerable<KeyValuePair<string, object>> actual)
        {
            if (expected == null && actual == null)
            {
                return;
            }

            if (expected == null || actual == null)
            {
                throw new EqualException(expected, actual);
            }

            if (ReferenceEquals(expected, actual))
            {
                return;
            }

            Assert.Equal(expected.Count(), actual.Count());

            // we do not care about the order of the log values
            expected = expected.OrderBy(kvp => kvp.Key);
            actual = actual.OrderBy(kvp => kvp.Key);

            Assert.Equal(expected, actual);
        }
    }
}
