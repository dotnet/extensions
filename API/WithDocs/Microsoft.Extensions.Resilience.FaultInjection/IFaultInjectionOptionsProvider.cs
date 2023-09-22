// Assembly 'Microsoft.Extensions.Resilience'

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Interface for fault-injection options provider implementations.
/// </summary>
/// <remarks>
/// A fault-injection options provider is intended to retain chaos policy configurations and
/// to allow <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.ChaosPolicyOptionsGroup" /> instances to be retrieved by other services.
/// </remarks>
public interface IFaultInjectionOptionsProvider
{
    /// <summary>
    /// Gets an instance of <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.ChaosPolicyOptionsGroup" /> from the provider by the options group name.
    /// </summary>
    /// <param name="optionsGroupName">The chaos policy options group name.</param>
    /// <param name="optionsGroup">
    /// The <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.ChaosPolicyOptionsGroup" /> associated with the options group name if it is found; otherwise, <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.ChaosPolicyOptionsGroup" /> associated with the options group name if it is found; otherwise, <see langword="false" />.
    /// </returns>
    bool TryGetChaosPolicyOptionsGroup(string optionsGroupName, [NotNullWhen(true)] out ChaosPolicyOptionsGroup? optionsGroup);
}
