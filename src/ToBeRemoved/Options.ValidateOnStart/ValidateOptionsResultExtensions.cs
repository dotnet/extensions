// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Options.Validation;

/// <summary>
/// Extension methods for helping with <see cref="ValidateOptionsResult"/> instances.
/// </summary>
public static class ValidateOptionsResultExtensions
{
    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the given result indicates a failure.
    /// </summary>
    /// <param name="result">The result value to inspect.</param>
    /// <exception cref="ValidationException">Thrown when the result indicates a failure.</exception>
    public static void ThrowIfFailed(this ValidateOptionsResult result)
    {
        _ = Throw.IfNull(result);

        if (result.Failed)
        {
            throw new ValidationException(result.FailureMessage);
        }
    }
}
