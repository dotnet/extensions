// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Validation = Microsoft.Extensions.Options.Validation;

namespace Microsoft.Extensions.Options.Validation.Test.Helpers;

public class ThreeFailuresMultiErrorValidator : IValidateOptions<NestedOptions>
{
    internal const string FirstErrorMessage = "First error message.";
    internal const string SecondErrorMessage = "Second error message.";
    internal const string ThirdErrorMessage = "Third error message.";
    internal const string ThirdPropertyName = "ThirdProperty";

    public ValidateOptionsResult Validate(string? name, NestedOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        builder.AddError(FirstErrorMessage);
        builder.AddError(SecondErrorMessage);
        builder.AddError(ThirdErrorMessage, ThirdPropertyName);

        return builder.Build();
    }
}
