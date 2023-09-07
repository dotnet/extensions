// Assembly 'Microsoft.Extensions.Options.Contextual'

using System;

namespace Microsoft.Extensions.Options.Contextual;

public interface IConfigureContextualOptions<in TOptions> : IDisposable where TOptions : class
{
    void Configure(TOptions options);
}
