// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Compliance.Testing;

internal sealed class FakeRedactorOptionsCustomValidator : IValidateOptions<FakeRedactorOptions>
{
    internal const int MaxNumberOfArgumentsForRedactionFormat = 1;

    public ValidateOptionsResult Validate(string? name, FakeRedactorOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        try
        {
            _ = string.Format(CultureInfo.InvariantCulture, options.RedactionFormat, "Test");
        }
        catch (FormatException ex)
        {
            builder.AddError(
                $"{nameof(options.RedactionFormat)} must be a valid .NET format string taking 0 or 1 arguments: {ex.Message}",
                nameof(options.RedactionFormat));

        }

        return builder.Build();
    }
}
