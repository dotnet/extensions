// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Logging;
using Xunit;

namespace Microsoft.AspNet.Testing.Logging
{
    public class NullLoggerTest
    {
        [Fact]
        public void BeginScope_CanDispose()
        {
            // Arrange
            var logger = NullLogger.Instance;

            // Act & Assert
            using (logger.BeginScope(null))
            {
            }
        }

        [Fact]
        public void IsEnabled_AlwaysFalse()
        {
            // Arrange
            var logger = NullLogger.Instance;

            // Act & Assert
            Assert.False(logger.IsEnabled(LogLevel.Debug));
            Assert.False(logger.IsEnabled(LogLevel.Verbose));
            Assert.False(logger.IsEnabled(LogLevel.Information));
            Assert.False(logger.IsEnabled(LogLevel.Warning));
            Assert.False(logger.IsEnabled(LogLevel.Error));
            Assert.False(logger.IsEnabled(LogLevel.Critical));
        }

        [Fact]
        public void Write_Does_Nothing()
        {
            // Arrange
            var logger = NullLogger.Instance;
            bool isCalled = false;

            // Act
            logger.Write(LogLevel.Verbose, eventId: 0, state: null, exception: null, formatter: (ex, message) => { isCalled = true; return string.Empty; });

            // Assert
            Assert.False(isCalled);
        }
    }
}