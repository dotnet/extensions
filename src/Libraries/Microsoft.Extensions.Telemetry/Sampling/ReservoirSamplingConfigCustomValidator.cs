// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// Validates CCKR configuration constraints that cannot be expressed with data annotations.
/// </summary>
internal sealed class ReservoirSamplingConfigCustomValidator : IValidateOptions<ReservoirSamplingConfig>
{
    public ValidateOptionsResult Validate(string? name, ReservoirSamplingConfig options)
    {
        ValidateOptionsResultBuilder result = new();

        if (options.FlushInterval <= TimeSpan.Zero)
        {
            result.AddError("FlushInterval must be greater than zero.", nameof(options.FlushInterval));
        }

        if (!Enum.IsDefined(options.UnseenWeightMode))
        {
            result.AddError("UnseenWeightMode must be a defined value.", nameof(options.UnseenWeightMode));
        }

        ValidateLogLevels(options.RetainAllLogLevels, result);
        ValidateCategoryPatterns(options.RetainAllCategories, nameof(options.RetainAllCategories), result);
        ValidateCategoryPatterns(options.SampledCategories, nameof(options.SampledCategories), result);

        return result.Build();
    }

    private static void ValidateLogLevels(IList<LogLevel>? levels, ValidateOptionsResultBuilder result)
    {
        if (levels is null)
        {
            return;
        }

        foreach (LogLevel level in levels)
        {
            if (!Enum.IsDefined(level))
            {
                result.AddError("RetainAllLogLevels must contain only defined values.", nameof(ReservoirSamplingConfig.RetainAllLogLevels));
            }
        }
    }

    private static void ValidateCategoryPatterns(
        IList<string>? patterns,
        string memberName,
        ValidateOptionsResultBuilder result)
    {
        if (patterns is null)
        {
            return;
        }

        foreach (string pattern in patterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                result.AddError("Category patterns cannot be empty.", memberName);
                continue;
            }

            int wildcard = pattern.IndexOf("*", StringComparison.Ordinal);
            if (wildcard >= 0 && pattern.IndexOf("*", wildcard + 1, StringComparison.Ordinal) >= 0)
            {
                result.AddError("Only one wildcard character is allowed in a category pattern.", memberName);
            }
        }
    }
}
#endif
