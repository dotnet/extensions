// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Microsoft.Shared.Text;
using Validation = Microsoft.Extensions.Options.Validation;

namespace Microsoft.Extensions.Compliance.Testing;

internal sealed class FakeRedactorOptionsCustomValidator : IValidateOptions<FakeRedactorOptions>
{
    internal const int MaxNumberOfArgumentsForRedactionFormat = 1;

    public ValidateOptionsResult Validate(string? name, FakeRedactorOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        if (!CompositeFormat.TryParse(options.RedactionFormat, out var compositeFormat, out var error))
        {
            builder.AddError(
                $"{nameof(options.RedactionFormat)} must be a valid .NET format string: {error}",
                nameof(options.RedactionFormat));
        }
        else if (compositeFormat.NumArgumentsNeeded > MaxNumberOfArgumentsForRedactionFormat)
        {
            builder.AddError(
                $"{nameof(options.RedactionFormat)} must take no more than {MaxNumberOfArgumentsForRedactionFormat} arguments. Currently found {compositeFormat.NumArgumentsNeeded}.",
                nameof(options.RedactionFormat));
        }

        return builder.Build();
    }
}
