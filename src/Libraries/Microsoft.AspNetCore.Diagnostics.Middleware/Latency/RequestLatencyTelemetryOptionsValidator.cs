// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.Latency;

[OptionsValidator]
internal sealed partial class RequestLatencyTelemetryOptionsValidator : IValidateOptions<RequestLatencyTelemetryOptions>
{
    /// <summary>
    /// Minimum possible timeout.
    /// </summary>
    internal const int MinimumTimeoutInMs = 1000;
}
