// Assembly 'Microsoft.Extensions.Resilience'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Class for exception policy options definition.
/// </summary>
public class ExceptionPolicyOptions : ChaosPolicyOptionsBase
{
    /// <summary>
    /// Gets or sets the exception key.
    /// </summary>
    /// <remarks>
    /// This key is used for fetching an exception instance
    /// from <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IExceptionRegistry" />.
    /// Default is set to "DefaultException".
    /// </remarks>
    public string ExceptionKey { get; set; }

    public ExceptionPolicyOptions();
}
