// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AsyncState;

/// <summary>
/// Represents an implementation of the <see cref="IAsyncContext{T}"/> interface.
/// </summary>
internal sealed class AsyncContext<T> : IAsyncLocalContext<T>
    where T : class
{
    private readonly AsyncStateToken _token;
    private readonly IAsyncState _state;

    public AsyncContext(IAsyncState state)
    {
        _state = state;
        _token = state.RegisterAsyncContext();
    }

    public T? Get() => (T?)_state.Get(_token);
    public void Set(T? context) => _state.Set(_token, context);

    public bool TryGet(out T? context)
    {
        var result = _state.TryGet(_token, out object? value);
        context = (T?)value;

        return result;
    }
}
