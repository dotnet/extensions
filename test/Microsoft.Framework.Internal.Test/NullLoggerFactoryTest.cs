using System;
using Microsoft.Framework.Logging;
using Xunit;

namespace Microsoft.Framework.Internal
{
    public class NullLoggerFactoryTest
    {
        [Fact]
        public void MinimumLevelIsVerbose()
        {
            Assert.True(LogLevel.Verbose == NullLoggerFactory.Instance.MinimumLevel);
        }

        [Fact]
        public void Create_GivesSameLogger()
        {
            var factory = NullLoggerFactory.Instance;

            var logger1 = factory.Create("Logger1");
            var logger2 = factory.Create("Logger2");

            Assert.True(logger1.GetHashCode() == logger2.GetHashCode());
        }
    }
}