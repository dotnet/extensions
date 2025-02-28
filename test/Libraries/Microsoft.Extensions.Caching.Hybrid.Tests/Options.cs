// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

internal static class Options
{
    public static IOptions<T> Create<T>(T value)
        where T : class
        => new OptionsImpl<T>(value);

    private sealed class OptionsImpl<T> : IOptions<T>
        where T : class
    {
        public OptionsImpl(T value)
        {
            Value = value;
        }

        public T Value { get; }
    }
}
