// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting.Testing.Internal;

internal sealed class FunctionDerivedInitializer : IStartupInitializer
{
    private readonly Func<IServiceProvider, CancellationToken, Task> _action;
    private readonly IServiceProvider _provider;

    public FunctionDerivedInitializer(IServiceProvider provider, Func<IServiceProvider, CancellationToken, Task> action)
    {
        _provider = provider;
        _action = action;
    }

    public Task InitializeAsync(CancellationToken token)
        => _action(_provider, token);
}
