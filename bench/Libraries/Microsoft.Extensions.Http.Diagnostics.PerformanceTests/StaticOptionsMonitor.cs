// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Diagnostics.Bench;

internal sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
{
    public StaticOptionsMonitor(T options)
    {
        CurrentValue = options;
    }

    public T CurrentValue { get; }

    public T Get(string? name)
        => CurrentValue;

    public IDisposable OnChange(Action<T, string> listener)
        => throw new NotSupportedException();
}
