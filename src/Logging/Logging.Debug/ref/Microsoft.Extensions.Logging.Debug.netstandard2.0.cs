namespace Microsoft.Extensions.Logging
{
    public static partial class DebugLoggerFactoryExtensions
    {
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddDebug(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { throw null; }
    }
}
namespace Microsoft.Extensions.Logging.Debug
{
    [Microsoft.Extensions.Logging.ProviderAliasAttribute("Debug")]
    public partial class DebugLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider, System.IDisposable
    {
        public DebugLoggerProvider() { }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name) { throw null; }
        public void Dispose() { }
    }
}
