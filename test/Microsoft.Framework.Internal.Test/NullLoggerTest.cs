using Microsoft.Framework.Logging;
using Xunit;

namespace Microsoft.Framework.Internal
{
    public class NullLoggerTest
    {
        [Fact]
        public void BeginScope_CanDispose()
        {
            using (NullLogger.Instance.BeginScope(null))
            {
            }
        }

        [Fact]
        public void IsEnabled_AlwaysFalse()
        {
            var logger = NullLogger.Instance;

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
            var logger = NullLogger.Instance;

            bool isCalled = false;
            logger.Write(LogLevel.Verbose, 0, null, null, (ex, message) => { isCalled = true; return string.Empty; });

            Assert.False(isCalled);
        }
    }
}