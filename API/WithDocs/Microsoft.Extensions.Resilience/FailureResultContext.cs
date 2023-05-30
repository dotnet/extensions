// Assembly 'Microsoft.Extensions.Resilience'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// Object model capturing the dimensions metered for a transient failure result.
/// </summary>
public readonly struct FailureResultContext
{
    /// <summary>
    /// Gets the source of the failure presented in delegate result.
    /// </summary>
    public string FailureSource { get; }

    /// <summary>
    /// Gets the reason of the failure presented in delegate result.
    /// </summary>
    public string FailureReason { get; }

    /// <summary>
    /// Gets additional information of the failure presented in delegate result.
    /// </summary>
    public string AdditionalInformation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Resilience.FailureResultContext" /> structure.
    /// </summary>
    /// <param name="failureSource">The source of the failure.</param>
    /// <param name="failureReason">The reason of the failure.</param>
    /// <param name="additionalInformation">Additional information for the failure.</param>
    /// <returns><see cref="T:Microsoft.Extensions.Resilience.FailureResultContext" /> object.</returns>
    public static FailureResultContext Create(string failureSource = "unknown", string failureReason = "unknown", string additionalInformation = "unknown");
}
