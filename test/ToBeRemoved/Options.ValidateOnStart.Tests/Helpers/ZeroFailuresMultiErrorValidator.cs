// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Validation = Microsoft.Extensions.Options.Validation;

namespace Microsoft.Extensions.Options.Validation.Test.Helpers;

public class ZeroFailuresMultiErrorValidator : IValidateOptions<NestedOptions>
{
    public ValidateOptionsResult Validate(string? name, NestedOptions options) => new ValidateOptionsResultBuilder().Build();
}
