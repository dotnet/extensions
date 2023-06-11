// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Default implementation for <see cref="IFaultInjectionOptionsProvider"/>.
/// </summary>
internal sealed class FaultInjectionOptionsProvider : IFaultInjectionOptionsProvider
{
    private readonly IOptionsMonitor<FaultInjectionOptions> _optionsMonitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="FaultInjectionOptionsProvider"/> class.
    /// </summary>
    /// <param name="optionsMonitor">
    /// The options monitor instance to retrieve chaos policy related configurations from.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Any of the parameters are <see langword="null"/>.
    /// </exception>
    public FaultInjectionOptionsProvider(IOptionsMonitor<FaultInjectionOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    /// <inheritdoc/>
    public bool TryGetChaosPolicyOptionsGroup(string optionsGroupName, [NotNullWhen(true)] out ChaosPolicyOptionsGroup? optionsGroup)
    {
        _ = Throw.IfNull(optionsGroupName);

        if (!_optionsMonitor.CurrentValue.ChaosPolicyOptionsGroups.TryGetValue(optionsGroupName, out optionsGroup))
        {
            return false;
        }

        return true;
    }
}
