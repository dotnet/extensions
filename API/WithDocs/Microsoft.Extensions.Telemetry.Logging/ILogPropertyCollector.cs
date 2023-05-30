// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Interface enabling custom providers of logging properties to report properties.
/// </summary>
/// <remarks>
/// See <see cref="M:Microsoft.Extensions.Telemetry.Logging.LogPropertiesAttribute.#ctor(System.Type,System.String)" /> for details on how this interface is used.
/// </remarks>
public interface ILogPropertyCollector
{
    /// <summary>
    /// Adds a property to the current log record.
    /// </summary>
    /// <param name="propertyName">The name of the property to add.</param>
    /// <param name="propertyValue">The value of the property to add.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="propertyName" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentException"><paramref name="propertyName" /> is empty or contains exclusively whitespace,
    /// or when a property of the same name has already been added.
    /// </exception>
    void Add(string propertyName, object? propertyValue);
}
