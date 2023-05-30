// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Telemetry.Logging;

public interface ILogPropertyCollector
{
    void Add(string propertyName, object? propertyValue);
}
