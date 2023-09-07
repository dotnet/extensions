// Assembly 'Microsoft.Extensions.Options.Contextual'

namespace Microsoft.Extensions.Options.Contextual;

/// <summary>
/// Interface used to validate options.
/// </summary>
/// <typeparam name="TOptions">The options type to validate.</typeparam>
public interface IValidateContextualOptions<in TOptions> where TOptions : class
{
    /// <summary>
    /// Validates a specific named options instance (or all when name is null).
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The options instance.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.Options.ValidateOptionsResult" /> result.</returns>
    ValidateOptionsResult Validate(string? name, TOptions options);
}
