// Assembly 'Microsoft.Extensions.AsyncState'

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AsyncState;

public interface IAsyncContext<T> where T : notnull
{
    T? Get();
    void Set(T? context);
    bool TryGet([MaybeNullWhen(false)] out T? context);
}
