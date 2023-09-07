// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System.CodeDom.Compiler;
using System.ComponentModel;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.AutoClient;

[EditorBrowsable(EditorBrowsableState.Never)]
[OptionsValidator]
public sealed class AutoClientOptionsValidator : IValidateOptions<AutoClientOptions>
{
    [GeneratedCode("Microsoft.Extensions.Options.SourceGeneration", "8.0.8.43109")]
    public ValidateOptionsResult Validate(string? name, AutoClientOptions options);
    public AutoClientOptionsValidator();
}
