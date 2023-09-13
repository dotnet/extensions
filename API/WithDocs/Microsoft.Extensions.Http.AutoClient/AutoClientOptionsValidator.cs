// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System.CodeDom.Compiler;
using System.ComponentModel;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Validator for <see cref="T:Microsoft.Extensions.Http.AutoClient.AutoClientOptions" />.
/// </summary>
/// <remarks>
/// This type is not intended to be directly invoked by application code.
/// It's intended to be invoked by generated code.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[OptionsValidator]
public sealed class AutoClientOptionsValidator : IValidateOptions<AutoClientOptions>
{
    /// <summary>
    /// Validates a specific named options instance (or all when <paramref name="name" /> is <see langword="null" />).
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The options instance.</param>
    /// <returns>Validation result.</returns>
    [GeneratedCode("Microsoft.Extensions.Options.SourceGeneration", "8.0.8.45707")]
    public ValidateOptionsResult Validate(string? name, AutoClientOptions options);

    public AutoClientOptionsValidator();
}
