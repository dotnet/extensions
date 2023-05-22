// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Validation = Microsoft.Extensions.Options.Validation;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

internal sealed class ResourceUtilizationTrackerOptionsManualValidator : IValidateOptions<ResourceUtilizationTrackerOptions>
{
    public ValidateOptionsResult Validate(string? name, ResourceUtilizationTrackerOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        if (options.CalculationPeriod > options.CollectionWindow)
        {
            builder.AddError(
                $"Value must be <= to {nameof(options.CollectionWindow)} ({options.CollectionWindow}), but is {options.CalculationPeriod}.",
                nameof(options.CalculationPeriod));
        }

        return builder.Build();
    }
}
