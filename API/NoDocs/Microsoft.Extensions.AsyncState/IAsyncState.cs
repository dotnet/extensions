// Assembly 'Microsoft.Extensions.AsyncState'

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AsyncState;

public interface IAsyncState
{
    void Initialize();
    void Reset();
    bool TryGet(AsyncStateToken token, [MaybeNullWhen(false)] out object? value);
    object? Get(AsyncStateToken token);
    void Set(AsyncStateToken token, object? value);
    AsyncStateToken RegisterAsyncContext();
}
