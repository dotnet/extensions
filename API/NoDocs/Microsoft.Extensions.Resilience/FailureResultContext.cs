// Assembly 'Microsoft.Extensions.Resilience'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Resilience;

public readonly struct FailureResultContext
{
    public string FailureSource { get; }
    public string FailureReason { get; }
    public string AdditionalInformation { get; }
    public static FailureResultContext Create(string failureSource = "unknown", string failureReason = "unknown", string additionalInformation = "unknown");
}
