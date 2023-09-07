// Assembly 'Microsoft.Extensions.Options.Contextual'

namespace Microsoft.Extensions.Options.Contextual;

/// <summary>
/// Used by contextual options providers to collect context data.
/// </summary>
public interface IOptionsContextReceiver
{
    /// <summary>
    /// Add a key-value pair to the context.
    /// </summary>
    /// <typeparam name="T">The type of the data.</typeparam>
    /// <param name="key">The name of the data.</param>
    /// <param name="value">The data used to determine how to populate contextual options.</param>
    void Receive<T>(string key, T value);
}
