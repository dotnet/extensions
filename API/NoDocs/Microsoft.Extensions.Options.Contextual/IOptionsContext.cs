// Assembly 'Microsoft.Extensions.Options.Contextual'

namespace Microsoft.Extensions.Options.Contextual;

public interface IOptionsContext
{
    void PopulateReceiver<T>(T receiver) where T : IOptionsContextReceiver;
}
