// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Default implementation for <see cref="IExceptionRegistry"/>.
/// </summary>
internal sealed class ExceptionRegistry : IExceptionRegistry
{
    private readonly IOptionsMonitor<FaultInjectionExceptionOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionRegistry"/> class.
    /// </summary>
    /// <param name="options">
    /// The <see cref="IOptionsMonitor{TOptions}"/> instance to retrieve <see cref="FaultInjectionExceptionOptions"/> from.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Any of the parameters are <see langword="null"/>.
    /// </exception>
    public ExceptionRegistry(IOptionsMonitor<FaultInjectionExceptionOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public Exception GetException(string key)
    {
        _ = Throw.IfNull(key);

        return _options.Get(key).Exception;
    }
}
