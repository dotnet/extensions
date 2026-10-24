// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using Polly;
using Polly.Retry;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Extensions for adding an HTTP retry strategy to <see cref="ResiliencePipelineBuilder{TResult}"/>.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Resilience, UrlFormat = DiagnosticIds.UrlFormat)]
public static class HttpRetryResiliencePipelineBuilderExtensions
{
    private static readonly HttpRetryBudgetOptionsValidator _budgetValidator = new();

    /// <summary>
    /// Adds a retry strategy configured by <see cref="HttpRetryStrategyOptions"/> to the builder, including the retry budget if one is configured.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="options">The retry strategy options.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <remarks>
    /// When <see cref="HttpRetryStrategyOptions.RetryBudget"/> is <see langword="null"/>, this is equivalent to
    /// <see cref="RetryResiliencePipelineBuilderExtensions.AddRetry{TResult}(ResiliencePipelineBuilder{TResult}, RetryStrategyOptions{TResult})"/>.
    /// Otherwise, a retry budget is applied to the retry strategy. See <see cref="HttpRetryBudgetOptions"/> for more details.
    /// <para>
    /// <see cref="RetryResiliencePipelineBuilderExtensions.AddRetry{TResult}(ResiliencePipelineBuilder{TResult}, RetryStrategyOptions{TResult})"/>
    /// ignores <see cref="HttpRetryStrategyOptions.RetryBudget"/>, so use this method to apply the budget in custom pipelines.
    /// </para>
    /// </remarks>
    /// <exception cref="ValidationException">Thrown when <paramref name="options"/> are invalid.</exception>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "All members of the budget strategy options are preserved by the DynamicDependency attribute.")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RetryBudgetStrategyOptions))]
    public static ResiliencePipelineBuilder<HttpResponseMessage> AddHttpRetry(this ResiliencePipelineBuilder<HttpResponseMessage> builder, HttpRetryStrategyOptions options)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(options);

        var budgetOptions = options.RetryBudget;

        if (budgetOptions is null)
        {
            return RetryResiliencePipelineBuilderExtensions.AddRetry(builder, options);
        }

        var validation = _budgetValidator.Validate($"{options.Name}.{nameof(HttpRetryStrategyOptions.RetryBudget)}", budgetOptions);
        if (validation.Failed)
        {
            throw new ValidationException(validation.FailureMessage);
        }

        // Snapshot the values so that later changes to the options instance don't affect the built pipelines.
        var retryRatio = budgetOptions.RetryRatio;
        var samplingDuration = budgetOptions.SamplingDuration;
        var minimumThroughput = budgetOptions.MinimumThroughput;

        _ = builder.AddStrategy(
            context => new RetryBudgetStrategy(new RetryBudget(retryRatio, samplingDuration, minimumThroughput, context.TimeProvider), context.Telemetry),
            new RetryBudgetStrategyOptions { Name = options.Name });

        return RetryResiliencePipelineBuilderExtensions.AddRetry(builder, CreateBudgetedCopy(options));
    }

    // The options instance can be shared by multiple pipelines (for example, with SelectPipelineByAuthority),
    // so the wrapped predicate goes into a copy instead of mutating the caller's instance.
    internal static HttpRetryStrategyOptions CreateBudgetedCopy(HttpRetryStrategyOptions options)
    {
        return new HttpRetryStrategyOptions
        {
            Name = options.Name,
            MaxRetryAttempts = options.MaxRetryAttempts,
            BackoffType = options.BackoffType,
            UseJitter = options.UseJitter,
            Delay = options.Delay,
            MaxDelay = options.MaxDelay,

            // The ShouldRetryAfterHeader setter overwrites DelayGenerator, so it must be assigned first.
            ShouldRetryAfterHeader = options.ShouldRetryAfterHeader,
            DelayGenerator = options.DelayGenerator,
            OnRetry = options.OnRetry,
            Randomizer = options.Randomizer,
            RetryBudget = options.RetryBudget,
            ShouldHandle = RetryBudgetStrategy.WrapShouldHandle(options.ShouldHandle, options.MaxRetryAttempts),
        };
    }
}
