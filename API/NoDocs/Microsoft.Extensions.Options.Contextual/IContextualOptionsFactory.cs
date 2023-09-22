// Assembly 'Microsoft.Extensions.Options.Contextual'

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Options.Contextual;

public interface IContextualOptionsFactory<TOptions> where TOptions : class
{
    ValueTask<TOptions> CreateAsync<TContext>(string name, in TContext context, CancellationToken cancellationToken) where TContext : IOptionsContext;
}
