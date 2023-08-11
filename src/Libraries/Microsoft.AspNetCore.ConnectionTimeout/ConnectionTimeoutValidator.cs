// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Connections;

internal sealed class ConnectionTimeoutValidator : IValidateOptions<ConnectionTimeoutOptions>
{
    /// <summary>
    /// Minimum possible timeout.
    /// </summary>
    private static readonly TimeSpan _minimumTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum possible timeout.
    /// </summary>
    private static readonly TimeSpan _maximumTimeout = TimeSpan.FromHours(1);

    public ValidateOptionsResult Validate(string? name, ConnectionTimeoutOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        if (options.Timeout < _minimumTimeout || options.Timeout > _maximumTimeout)
        {
            builder.AddError(
                "must be in the range [{_minimumTimeout}..{_maximumTimeout}].",
                nameof(options.Timeout));
        }

        return builder.Build();
    }
}
