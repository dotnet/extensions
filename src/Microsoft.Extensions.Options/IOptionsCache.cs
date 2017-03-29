// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Used to cache TOptions instances.
    /// </summary>
    /// <typeparam name="TOptions">The type of options being requested.</typeparam>
    public interface IOptionsCache<TOptions> where TOptions : class
    {
        TOptions GetOrAdd(string name, Func<TOptions> createOptions);

        bool TryAdd(string name, TOptions options);

        bool TryRemove(string name);

        // Do we need a Clear all?
    }
}