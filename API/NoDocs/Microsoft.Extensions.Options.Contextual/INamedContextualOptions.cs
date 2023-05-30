// Assembly 'Microsoft.Extensions.Options.Contextual'

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Options.Contextual;

public interface INamedContextualOptions<TOptions> : IContextualOptions<TOptions> where TOptions : class
{
    ValueTask<TOptions> GetAsync<TContext>(string name, in TContext context, CancellationToken cancellationToken) where TContext : IOptionsContext;
}
