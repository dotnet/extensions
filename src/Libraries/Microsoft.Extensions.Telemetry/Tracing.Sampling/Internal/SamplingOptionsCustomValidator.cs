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
                    "Sampler type is set for trace Id ratio based " +
                    "but options are not set for it."),
            SamplerType.ParentBased
                when o.ParentBasedSamplerOptions is null =>
                ValidateOptionsResult.Fail(
                    "Sampler type is set for parent based " +
                    "but options are not set for it."),
            SamplerType.ParentBased
                when o.ParentBasedSamplerOptions.RootSamplerType == SamplerType.ParentBased =>
                ValidateOptionsResult.Fail(
                    "Sampler type is set for parent based " +
                    "but the root sampler is also set to parent based."),
            SamplerType.ParentBased
                when o.ParentBasedSamplerOptions.RootSamplerType == SamplerType.TraceIdRatioBased
                  && o.TraceIdRatioBasedSamplerOptions is null =>
                ValidateOptionsResult.Fail(
                    "Sampler type is set for parent based with trace Id ratio based root sampler " +
                    "but the trace Id ratio based options are not set."),
            SamplerType st when !Enum.IsDefined(typeof(SamplerType), st) =>
                ValidateOptionsResult.Fail(
                    $"Unknown sampler type '{st}'."),
            _ =>
                ValidateOptionsResult.Success
        };
}
