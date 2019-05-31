using Xunit;

namespace Microsoft.Extensions.Logging.AzureEventHubs.Tests
{
    public class AzureEventHubsLoggerTests
    {
        private const string _loggerName = "test";

        private static AzureEventHubsLogger SetUp(AzureEventHubsLoggerOptions options = null)
        {
            // Arrange
            var formatter = new DefaultAzureEventHubsLoggerFormatter();
            var logger = new AzureEventHubsLogger(_loggerName, formatter, null);
            logger.ScopeProvider = new LoggerExternalScopeProvider();
            logger.Options = options ?? new AzureEventHubsLoggerOptions();
            return logger;
        }

        [Fact]
        public static void IsEnabledReturnsCorrectValue()
        {
            var logger = SetUp();

            Assert.False(logger.IsEnabled(LogLevel.None));
            Assert.True(logger.IsEnabled(LogLevel.Critical));
            Assert.True(logger.IsEnabled(LogLevel.Error));
            Assert.True(logger.IsEnabled(LogLevel.Warning));
            Assert.True(logger.IsEnabled(LogLevel.Information));
            Assert.True(logger.IsEnabled(LogLevel.Debug));
            Assert.True(logger.IsEnabled(LogLevel.Trace));
        }
    }
}
