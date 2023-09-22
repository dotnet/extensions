// Assembly 'Microsoft.Extensions.Options.Contextual'

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Options.Contextual;

/// <summary>
/// Used to retrieve named configuration data from a contextual options provider implementation.
/// </summary>
/// <typeparam name="TOptions">The type of options configured.</typeparam>
public interface ILoadContextualOptions<TOptions> where TOptions : class
{
    /// <summary>
    /// Gets the data to configure an instance of <typeparamref name="TOptions" />.
    /// </summary>
    /// <typeparam name="TContext">A type defining the context for this request.</typeparam>
    /// <param name="name">The name of the options to configure.</param>
    /// <param name="context">The context that will be used to configure the options.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An object to configure an instance of <typeparamref name="TOptions" />.</returns>
    ValueTask<IConfigureContextualOptions<TOptions>> LoadAsync<TContext>(string name, in TContext context, CancellationToken cancellationToken) where TContext : IOptionsContext;
}
