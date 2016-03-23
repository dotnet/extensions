// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging.Debug;
using Xunit;

namespace Microsoft.Extensions.Logging
{
    public class DebugLoggerTest
    {
        [Fact]
        public void CallingBeginScopeOnLogger_AlwaysReturnsNewDisposableInstance()
        {
            // Arrange
            var logger = new DebugLogger("Test");

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
            var logger = new DebugLogger("Test");

            // Act
            var disposable = logger.BeginScopeImpl("Scope1");

            // Assert
            Assert.NotNull(disposable);
        }

        [Fact]
        public void CallingLogWithCurlyBracesAfterFormatter_DoesNotThrow()
        {
            // Arrange
            var logger = new DebugLogger("Test");
            var message = "{test string}";

            // Act
            logger.Log(LogLevel.Debug, 0, message, null, (s, e) => s);
        }
    }
}
