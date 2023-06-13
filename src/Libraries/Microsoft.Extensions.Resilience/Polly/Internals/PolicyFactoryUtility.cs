// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Resilience.Options;
using Polly;

#pragma warning disable R9A061

namespace Microsoft.Extensions.Resilience.Internal;

internal sealed class PolicyFactoryUtility
{
    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="ValidationException"><paramref name="options"/> are not valid.</exception>
    public static void ValidateOptions<TOptions>(IValidateOptions<TOptions> validator, TOptions options)
        where TOptions : class, new()
    {
        validator.Validate(null, options).ThrowIfFailed();
    }

    public static Action<Context> OnCircuitReset(string policyName, CircuitBreakerPolicyOptions options,
        string eventType, ILogger logger, Action<string, string, Context> recordMetric)
    {
        return (context) =>
        {
            Log.LogCircuitReset(logger, policyName);
            recordMetric(eventType, policyName, context);

            options.OnCircuitReset(new ResetActionArguments(context, CancellationToken.None));
        };
    }

    public static Task OnTimeoutAsync(Context context, string policyName, TimeoutPolicyOptions options,
        ILogger logger, Action<string, string, Context> recordMetric)
    {
        Log.LogTimeout(logger, policyName);
        recordMetric(PolicyEvents.TimeoutPolicyEvent, policyName, context);

        return options.OnTimedOutAsync(new TimeoutTaskArguments(context, CancellationToken.None));
    }

    public static Task OnBulkheadRejectedAsync(Context context, string policyName, BulkheadPolicyOptions options, ILogger logger, Action<string, string, Context> recordMetric)
    {
        Log.LogBulkhead(logger, policyName);
        recordMetric(PolicyEvents.BulkheadPolicyEvent, policyName, context);
        return options.OnBulkheadRejectedAsync(new BulkheadTaskArguments(context, CancellationToken.None));
    }
}
