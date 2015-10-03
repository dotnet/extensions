// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using Moq;
#endif
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class LoggerFactoryExtensionsTest
    {
#if DNX451
        [Fact] 
        public void LoggerFactoryCreateOfT_CallsCreateWithCorrectName()
        {
            // Arrange
            var expected = typeof(TestType).FullName;

            var factory = new Mock<ILoggerFactory>();
            factory.Setup(f => f.CreateLogger(
                It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

            // Act
            factory.Object.CreateLogger<TestType>();

            // Assert
            factory.Verify(f => f.CreateLogger(expected));
        }

        [Fact]
        public void LoggerFactoryCreateOfT_SingleGeneric_CallsCreateWithCorrectName()
        {
            // Arrange
            var factory = new Mock<ILoggerFactory>();
            factory.Setup(f => f.CreateLogger(It.Is<string>(
                x => x.Equals("Microsoft.Extensions.Logging.Test.GenericClass<Microsoft.Extensions.Logging.Test.TestType>"))))
            .Returns(new Mock<ILogger>().Object);

            var logger = factory.Object.CreateLogger<GenericClass<TestType>>();

            // Assert
            Assert.NotNull(logger);
        }

        [Fact]
        public void LoggerFactoryCreateOfT_TwoGenerics_CallsCreateWithCorrectName()
        {
            // Arrange
            var factory = new Mock<ILoggerFactory>();
            factory.Setup(f => f.CreateLogger(It.Is<string>(
                x => x.Equals("Microsoft.Extensions.Logging.Test.GenericClass<Microsoft.Extensions.Logging.Test.TestType, Microsoft.Extensions.Logging.Test.SecondTestType>"))))
            .Returns(new Mock<ILogger>().Object);

            var logger = factory.Object.CreateLogger<GenericClass<TestType,SecondTestType>>();

            // Assert
            Assert.NotNull(logger);
        }
#endif
    }

    internal class TestType
    {
        // intentionally holds nothing
    }

    internal class SecondTestType
    {
        // intentionally holds nothing
    }

    internal class GenericClass<X, Y> where X : class where Y : class
    {
        // intentionally holds nothing
    }

    internal class GenericClass<X> where X : class
    {
        // intentionally holds nothing
    }
}
