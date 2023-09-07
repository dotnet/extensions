// Assembly 'Microsoft.Extensions.Options.Contextual'

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Options.Contextual;

public interface ILoadContextualOptions<TOptions> where TOptions : class
{
    ValueTask<IConfigureContextualOptions<TOptions>> LoadAsync<TContext>(string name, in TContext context, CancellationToken cancellationToken) where TContext : IOptionsContext;
}
