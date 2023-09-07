// Assembly 'Microsoft.Extensions.Options.Contextual'

namespace Microsoft.Extensions.Options.Contextual;

/// <summary>
/// Represents something that configures the <typeparamref name="TOptions" /> type.
/// </summary>
/// <typeparam name="TOptions">Options type being configured.</typeparam>
public interface IPostConfigureContextualOptions<in TOptions> where TOptions : class
{
    /// <summary>
    /// Invoked to configure a <typeparamref name="TOptions" /> instance.
    /// </summary>
    /// <typeparam name="TContext">Options type being configured.</typeparam>
    /// <param name="name">The name of the options instance being configured.</param>
    /// <param name="context">The context that will be used to configure the options.</param>
    /// <param name="options">The options instance to configured.</param>
    void PostConfigure<TContext>(string name, in TContext context, TOptions options) where TContext : IOptionsContext;
}
