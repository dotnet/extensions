// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Options.Validation.Test;

internal class AnotherNestedOptionsValidator : IValidateOptions<NestedOptions>
{
    public const string LogMethod = "Executed Validator.";
    private readonly ILogger<NestedOptionsValidator> _logger;

    public AnotherNestedOptionsValidator(ILogger<NestedOptionsValidator> logger)
    {
        _logger = logger;
    }

    public ValidateOptionsResult Validate(string? name, NestedOptions options)
    {
        _logger.Log(LogLevel.Information, 0, LogMethod);

        return ValidateOptionsResult.Success;
    }
}
