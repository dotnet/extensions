// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using Moq;
#endif
using Xunit;

namespace Microsoft.Framework.Logging.Test
{
    public class LoggerFactoryExtensionsTest
    {
#if ASPNET50
        [Fact] 
        public void LoggerFactoryCreateOfT_CallsCreateWithCorrectName()
        {
            // Arrange
            var expected = typeof(TestType).FullName;

            var factory = new Mock<ILoggerFactory>();
            factory.Setup(f => f.Create(
                It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

            // Act
            factory.Object.Create<TestType>();

            // Assert
            factory.Verify(f => f.Create(expected));
        }
#endif

        private class TestType
        {
            // intentionally holds nothing
        }
    }
}