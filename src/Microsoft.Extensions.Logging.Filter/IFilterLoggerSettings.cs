using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging.Filter
{
    public interface IFilterLoggerSettings
    {
        IChangeToken ChangeToken { get; }

        bool TryGetSwitch(string name, out LogLevel level);

        IFilterLoggerSettings Reload();
    }
}