using Microsoft.Extensions.Logging.AzureAppServices.Internal;
using Moq;
using Serilog.Events;
using Xunit;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test
{
    public class WebConfigurationLevelSwitchTests
    {
        [Fact]
        public void InitializesWithCurrentLevelWhenCreated()
        {
            var configurationReader = new Mock<IWebAppLogConfigurationReader>();
            configurationReader.SetupGet(c => c.Current).Returns(new WebAppLogConfiguration(
                isRunningInWebApp: true,
                fileLoggingEnabled: true,
                fileLoggingLevel: LogLevel.Warning,
                fileLoggingFolder: "",
                blobLoggingEnabled: true,
                blobLoggingLevel: LogLevel.Warning,
                blobContainerUrl: ""));
            var levelSwitch = new WebConfigurationReaderLevelSwitch(configurationReader.Object, configuration => configuration.BlobLoggingLevel);

            Assert.Equal(LogEventLevel.Warning, levelSwitch.MinimumLevel);
        }
    }
}
