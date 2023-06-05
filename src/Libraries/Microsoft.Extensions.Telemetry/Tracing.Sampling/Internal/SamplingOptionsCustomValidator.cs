// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Telemetry.Tracing.Internal;

internal sealed class SamplingOptionsCustomValidator : IValidateOptions<SamplingOptions>
{
    public ValidateOptionsResult Validate(string? name, SamplingOptions o) =>
        o.SamplerType switch
        {
            SamplerType.TraceIdRatioBased
                when o.TraceIdRatioBasedSamplerOptions is null =>
                ValidateOptionsResult.Fail(
                    "Sampler type is set for Trace Id Ratio Based " +
                    "but options are not set for it."),
            SamplerType.ParentBased
                when o.ParentBasedSamplerOptions is null =>
                ValidateOptionsResult.Fail(
                    "Sampler type is set for Parent Based " +
                    "but options are not set for it."),
            SamplerType.ParentBased
                when o.ParentBasedSamplerOptions.RootSamplerType == SamplerType.ParentBased =>
                ValidateOptionsResult.Fail(
                    "Sampler type is set for Parent Based " +
                    "but the Root Sampler is also set to Parent Based."),
            SamplerType.ParentBased
                when o.ParentBasedSamplerOptions.RootSamplerType == SamplerType.TraceIdRatioBased
                  && o.ParentBasedSamplerOptions.TraceIdRatioBasedSamplerOptions is null
                  && o.TraceIdRatioBasedSamplerOptions is not null =>
                ValidateOptionsResult.Fail(
                    "Sampler type is set for Parent Based with Trace Id Ratio Based as the Root Sampler " +
                    "but the Trace Id Ratio Based options are set in Sampling Options instead of Parent Based options. " +
                    "Trace Id Ratio Based options should be set in Parent Based options."),
            SamplerType.ParentBased
                when o.ParentBasedSamplerOptions.RootSamplerType == SamplerType.TraceIdRatioBased
                  && o.ParentBasedSamplerOptions.TraceIdRatioBasedSamplerOptions is null =>
                ValidateOptionsResult.Fail(
                    "Sampler type is set for Parent Based with Trace Id Ratio Based as the Root Sampler " +
                    "but the Trace Id Ratio Based options are not set."),
            SamplerType st when !Enum.IsDefined(typeof(SamplerType), st) =>
                ValidateOptionsResult.Fail(
                    $"Unknown sampler type '{st}'."),
            _ =>
                ValidateOptionsResult.Success
        };
}
