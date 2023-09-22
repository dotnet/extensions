// Assembly 'Microsoft.Extensions.Options.Contextual'

namespace Microsoft.Extensions.Options.Contextual;

public interface IOptionsContextReceiver
{
    void Receive<T>(string key, T value);
}
