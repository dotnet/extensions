// Assembly 'Microsoft.Extensions.Options.Contextual'

namespace Microsoft.Extensions.Options.Contextual;

public interface IValidateContextualOptions<in TOptions> where TOptions : class
{
    ValidateOptionsResult Validate(string? name, TOptions options);
}
