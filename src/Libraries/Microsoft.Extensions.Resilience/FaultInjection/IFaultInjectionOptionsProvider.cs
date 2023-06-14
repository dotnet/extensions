// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Interface for fault-injection options provider implementations.
/// </summary>
/// <remarks>
/// A fault-injection options provider is intended to retain chaos policy configurations and
/// to allow <see cref="ChaosPolicyOptionsGroup"/> instances to be retrieved by other services.
/// </remarks>
public interface IFaultInjectionOptionsProvider
{
    /// <summary>
    /// Get an instance of <see cref="ChaosPolicyOptionsGroup"/> from the provider by the options group name.
    /// </summary>
    /// <param name="optionsGroupName">The chaos policy options group name.</param>
    /// <param name="optionsGroup">
    /// The <see cref="ChaosPolicyOptionsGroup"/> associated with the options group name if it is found; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// True if the <see cref="ChaosPolicyOptionsGroup"/> associated with the options group name if it is found; otherwise, false.
    /// </returns>
    public bool TryGetChaosPolicyOptionsGroup(string optionsGroupName, [NotNullWhen(true)] out ChaosPolicyOptionsGroup? optionsGroup);
}
