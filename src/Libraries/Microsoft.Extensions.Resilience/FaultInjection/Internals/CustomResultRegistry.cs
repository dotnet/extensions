// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Default implementation for <see cref="ICustomResultRegistry"/>.
/// </summary>
internal sealed class CustomResultRegistry : ICustomResultRegistry
{
    private readonly IOptionsMonitor<FaultInjectionCustomResultOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomResultRegistry"/> class.
    /// </summary>
    /// <param name="options">
    /// The <see cref="IOptionsMonitor{TOptions}"/> instance to retrieve <see cref="FaultInjectionCustomResultOptions"/> from.
    /// </param>
    public CustomResultRegistry(IOptionsMonitor<FaultInjectionCustomResultOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public object GetCustomResult(string key)
    {
        return _options.Get(key).CustomResult;
    }
}
