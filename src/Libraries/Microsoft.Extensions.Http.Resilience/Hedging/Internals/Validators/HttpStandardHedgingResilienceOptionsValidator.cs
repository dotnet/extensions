// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience.Internal.Validators;

internal sealed class HttpStandardHedgingResilienceOptionsValidator : IValidateOptions<HttpStandardHedgingResilienceOptions>
{
    public ValidateOptionsResult Validate(string? name, HttpStandardHedgingResilienceOptions options) => ValidateOptionsResult.Success;
}
