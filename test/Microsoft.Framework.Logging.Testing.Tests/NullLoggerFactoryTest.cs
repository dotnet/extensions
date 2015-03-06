// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Framework.Logging.Testing
{
    public class NullLoggerFactoryTest
    {
        [Fact]
        public void MinimumLevelIsVerbose()
        {
            // Act & Assert
            Assert.True(LogLevel.Verbose == NullLoggerFactory.Instance.MinimumLevel);
        }

        [Fact]
        public void Create_GivesSameLogger()
        {
            // Arrange
            var factory = NullLoggerFactory.Instance;

            // Act
            var logger1 = factory.CreateLogger("Logger1");
            var logger2 = factory.CreateLogger("Logger2");

            // Assert
            Assert.Same(logger1, logger2);
        }
    }
}