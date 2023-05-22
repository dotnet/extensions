// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HeaderParsing;

internal sealed class HeaderParsingOptionsManualValidator : IValidateOptions<HeaderParsingOptions>
{
    public ValidateOptionsResult Validate(string? name, HeaderParsingOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        foreach (var item in options.MaxCachedValuesPerHeader)
        {
            if (item.Value < 0)
            {
                builder.AddError(
                    $"Negative cached value count of {item.Value} specified for the {item.Key} header",
                    nameof(options.MaxCachedValuesPerHeader));
            }
        }

        return builder.Build();
    }
}
