// Assembly 'Microsoft.Extensions.Options.Contextual'

namespace Microsoft.Extensions.Options.Contextual;

public interface IPostConfigureContextualOptions<in TOptions> where TOptions : class
{
    void PostConfigure<TContext>(string name, in TContext context, TOptions options) where TContext : IOptionsContext;
}
