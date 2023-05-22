// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Metering;

namespace Microsoft.Extensions.Resilience;

internal static partial class PollyMetric
{
    [Counter(
        ResilienceDimensions.PipelineName,
        ResilienceDimensions.PipelineKey,
        ResilienceDimensions.ResultType,
        ResilienceDimensions.PolicyName,
        ResilienceDimensions.EventName,
        ResilienceDimensions.FailureSource,
        ResilienceDimensions.FailureReason,
        ResilienceDimensions.FailureSummary,
        ResilienceDimensions.DependencyName,
        ResilienceDimensions.RequestName,
        Name = @"R9\Resilience\Policies")]
    public static partial PoliciesMetricCounter CreatePoliciesMetricCounter(Meter meter);
}
