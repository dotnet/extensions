// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Resilience.Internal;
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
internal sealed class NoopChangeToken : IChangeToken
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
    private readonly NoopDisposable _noopRegistration = new();

    public bool HasChanged => false;

    public bool ActiveChangeCallbacks => true;

    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => _noopRegistration;

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
            // No op
        }
    }
}
