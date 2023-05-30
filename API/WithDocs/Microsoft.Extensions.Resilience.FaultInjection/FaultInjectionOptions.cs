// Assembly 'Microsoft.Extensions.Resilience'

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Class to contain fault injection options provider option values loaded from configuration sources.
/// </summary>
public class FaultInjectionOptions
{
    /// <summary>
    /// Gets or sets the dictionary that stores <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.ChaosPolicyOptionsGroup" />.
    /// </summary>
    public IDictionary<string, ChaosPolicyOptionsGroup> ChaosPolicyOptionsGroups { get; set; }

    public FaultInjectionOptions();
}
