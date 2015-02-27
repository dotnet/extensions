// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Logging;
using Xunit;

namespace Microsoft.AspNet.Testing.Logging
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
            var logger1 = factory.Create("Logger1");
            var logger2 = factory.Create("Logger2");

            // Assert
            Assert.Same(logger1, logger2);
        }
    }
}