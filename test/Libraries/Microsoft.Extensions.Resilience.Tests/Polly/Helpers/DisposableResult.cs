// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Resilience.Polly.Test.Helpers;

internal sealed class DisposableResult : IDisposable
{
    public readonly TaskCompletionSource<bool> OnDisposed = new();

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;

        OnDisposed.TrySetResult(true);
    }
}
