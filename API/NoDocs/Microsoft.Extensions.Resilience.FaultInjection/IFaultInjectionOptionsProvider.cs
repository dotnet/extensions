// Assembly 'Microsoft.Extensions.Resilience'

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Resilience.FaultInjection;

public interface IFaultInjectionOptionsProvider
{
    bool TryGetChaosPolicyOptionsGroup(string optionsGroupName, [NotNullWhen(true)] out ChaosPolicyOptionsGroup? optionsGroup);
}
