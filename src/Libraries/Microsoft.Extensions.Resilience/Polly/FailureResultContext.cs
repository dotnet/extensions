// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// Object model capturing the dimensions metered for a transient failure result.
/// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types (Such usage is not expected in this scenario)
public readonly struct FailureResultContext
#pragma warning restore CA1815
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailureResultContext" /> structure.
    /// </summary>
    /// <param name="failureSource">The source of the failure.</param>
    /// <param name="failureReason">The reason of the failure.</param>
    /// <param name="additionalInformation">Additional information for the failure.</param>
    /// <returns><see cref="FailureResultContext"/> object.</returns>
    public static FailureResultContext Create(
        string failureSource = TelemetryConstants.Unknown,
        string failureReason = TelemetryConstants.Unknown,
        string additionalInformation = TelemetryConstants.Unknown)
       => new(failureSource, failureReason, additionalInformation);

    private FailureResultContext(string failureSource, string failureReason, string additionalInformation)
    {
        FailureSource = Throw.IfNullOrEmpty(failureSource);
        FailureReason = Throw.IfNullOrEmpty(failureReason);
        AdditionalInformation = Throw.IfNullOrEmpty(additionalInformation);
    }

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
}
