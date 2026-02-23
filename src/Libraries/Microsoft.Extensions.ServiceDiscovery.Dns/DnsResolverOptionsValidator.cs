// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

internal sealed class DnsResolverOptionsValidator : IValidateOptions<DnsResolverOptions>
{
    // CancellationTokenSource.CancelAfter has a maximum timeout of Int32.MaxValue milliseconds.
    private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

    public ValidateOptionsResult Validate(string? name, DnsResolverOptions options)
    {
        if (options.Servers is null)
        {
            return ValidateOptionsResult.Fail($"{nameof(options.Servers)} must not be null.");
        }

        if (options.MaxAttempts < 1)
        {
            return ValidateOptionsResult.Fail($"{nameof(options.MaxAttempts)} must be one or greater.");
        }

        if (options.Timeout != Timeout.InfiniteTimeSpan)
        {
            if (options.Timeout <= TimeSpan.Zero)
            {
                return ValidateOptionsResult.Fail($"{nameof(options.Timeout)} must not be negative or zero.");
            }

            if (options.Timeout > s_maxTimeout)
            {
                return ValidateOptionsResult.Fail($"{nameof(options.Timeout)} must not be greater than {s_maxTimeout.TotalMilliseconds} milliseconds.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
