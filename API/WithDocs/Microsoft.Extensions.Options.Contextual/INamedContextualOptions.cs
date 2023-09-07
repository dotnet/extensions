// Assembly 'Microsoft.Extensions.Options.Contextual'

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Options.Contextual;

/// <summary>
/// Used to retrieve named configured <typeparamref name="TOptions" /> instances.
/// </summary>
/// <typeparam name="TOptions">The type of options being requested.</typeparam>
public interface INamedContextualOptions<TOptions> : IContextualOptions<TOptions> where TOptions : class
{
    /// <summary>
    /// Gets the named configured <typeparamref name="TOptions" /> instance.
    /// </summary>
    /// <typeparam name="TContext">A type defining the context for this request.</typeparam>
    /// <param name="name">The name of the options to get.</param>
    /// <param name="context">The context that will be used to create the options.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A configured instance of <typeparamref name="TOptions" />.</returns>
    ValueTask<TOptions> GetAsync<TContext>(string name, in TContext context, CancellationToken cancellationToken) where TContext : IOptionsContext;
}
