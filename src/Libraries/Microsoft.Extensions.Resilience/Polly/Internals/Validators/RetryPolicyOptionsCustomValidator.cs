// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Options;

namespace Microsoft.Extensions.Resilience.Internal;

internal sealed class RetryPolicyOptionsCustomValidator : IValidateOptions<RetryPolicyOptions>
{
    [UnconditionalSuppressMessage(
    "Trimming",
    "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
    Justification = "Addressed with [DynamicallyAddressedMembers]")]
    public ValidateOptionsResult Validate(string? name, RetryPolicyOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();
        var context = new ValidationContext(options);

        if (options.RetryCount == RetryPolicyOptions.InfiniteRetry && options.BackoffType != BackoffType.Constant)
        {
            builder.AddError($"must be {BackoffType.Constant} when infinite retries are enabled.", nameof(options.BackoffType));
        }

        if (options.RetryCount != RetryPolicyOptions.InfiniteRetry)
        {
            int position = -1;
            foreach (var retryDelay in options.GetDelays())
            {
                position++;
                ValidateRetryDelay(builder, position, retryDelay);
            }
        }

        return builder.Build();
    }

    private static void ValidateRetryDelay(ValidateOptionsResultBuilder builder, int attempt, TimeSpan value)
    {
        var retryDelay = (long)value.TotalMilliseconds;
        if (retryDelay > int.MaxValue)
        {
            builder.AddError(
                $"unable to validate retry delay #{attempt} = {retryDelay}. Must be a positive TimeSpan and less than {int.MaxValue} milliseconds long.",
                nameof(RetryPolicyOptions.RetryCount));
        }
    }
}
