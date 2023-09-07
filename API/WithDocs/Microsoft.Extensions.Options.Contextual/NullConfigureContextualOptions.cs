// Assembly 'Microsoft.Extensions.Options.Contextual'

namespace Microsoft.Extensions.Options.Contextual;

/// <summary>
/// Helper class.
/// </summary>
public static class NullConfigureContextualOptions
{
    /// <summary>
    /// Gets a singleton instance of <see cref="T:Microsoft.Extensions.Options.Contextual.NullConfigureContextualOptions`1" />.
    /// </summary>
    /// <typeparam name="TOptions">The options type to configure.</typeparam>
    /// <returns>A do-nothing instance of <see cref="T:Microsoft.Extensions.Options.Contextual.IConfigureContextualOptions`1" />.</returns>
    public static IConfigureContextualOptions<TOptions> GetInstance<TOptions>() where TOptions : class;
}
