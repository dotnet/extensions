// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Options.Validation.Test;

internal class FailingNestedOptionsValidator : IValidateOptions<NestedOptions>
{
    public ValidateOptionsResult Validate(string? name, NestedOptions options)
        => options.Integer > 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("Validation failed for options with name: " + name);
}
