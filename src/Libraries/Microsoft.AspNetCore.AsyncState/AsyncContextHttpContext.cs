// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AsyncState;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.AsyncState;

#pragma warning disable EXTEXP0006 // Type is for evaluation purposes only and is subject to change or removal in future updates
internal sealed class AsyncContextHttpContext<T> : IAsyncContext<T>
    where T : class
{
    private readonly IAsyncLocalContext<T> _localContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AsyncContextHttpContext(
        IAsyncLocalContext<T> localContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _localContext = localContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public T? Get()
    {
        if (!TryGet(out var value))
        {
            Throw.InvalidOperationException("Async context not available");
        }

        return value;
    }

    public void Set(T? value)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            // the call is made outside of an HTTP context
            _localContext.Set(value);
            return;
        }

        httpContext.Features[typeof(TypeWrapper<T>)] = value;
    }

    public bool TryGet(out T? value)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            // the call is made outside of an HTTP context
            return _localContext.TryGet(out value);
        }

        value = (T?)httpContext.Features[typeof(TypeWrapper<T>)];
        return true;
    }
}
#pragma warning restore EXTEXP0006 // Type is for evaluation purposes only and is subject to change or removal in future updates
