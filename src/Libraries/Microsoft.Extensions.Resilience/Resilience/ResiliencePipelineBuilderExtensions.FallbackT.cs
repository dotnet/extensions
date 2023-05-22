// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience;

public static partial class ResiliencePipelineBuilderExtensions
{
    /// <summary>
    /// Adds a fallback policy with default options to a pipeline.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <param name="builder">The policy pipeline builder.</param>
    /// <param name="policyName">The policy name.</param>
    /// <param name="provider">The task performed in the fallback scenario when the initial execution encounters a transient failure.</param>
    /// <returns>Current instance.</returns>
    public static IResiliencePipelineBuilder<TResult> AddFallbackPolicy<TResult>(
        this IResiliencePipelineBuilder<TResult> builder,
        string policyName,
        FallbackScenarioTaskProvider<TResult> provider)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(provider);

        return builder.AddFallbackPolicyInternal(policyName, provider, null, null);
    }

    /// <summary>
    /// Adds a fallback policy to a pipeline.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <param name="builder">The policy pipeline builder.</param>
    /// <param name="policyName">The policy name.</param>
    /// <param name="provider">The task performed in the fallback scenario when the initial execution encounters a transient failure.</param>
    /// <param name="configure">The action that configures the default policy options.</param>
    /// <returns>Current instance.</returns>
    public static IResiliencePipelineBuilder<TResult> AddFallbackPolicy<TResult>(
        this IResiliencePipelineBuilder<TResult> builder,
        string policyName,
        FallbackScenarioTaskProvider<TResult> provider,
        Action<FallbackPolicyOptions<TResult>> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(provider);
        _ = Throw.IfNull(configure);

        return builder.AddFallbackPolicyInternal(policyName, provider, null, configure);
    }

    /// <summary>
    /// Adds a fallback policy to a pipeline.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <param name="builder">The policy pipeline builder.</param>
    /// <param name="policyName">The policy name.</param>
    /// <param name="provider">The task performed in the fallback scenario when the initial execution encounters a transient failure.</param>
    /// <param name="section">The configuration that the options will bind against.</param>
    /// <returns>Current instance.</returns>
    public static IResiliencePipelineBuilder<TResult> AddFallbackPolicy<TResult>(
        this IResiliencePipelineBuilder<TResult> builder,
        string policyName,
        FallbackScenarioTaskProvider<TResult> provider,
        IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(provider);
        _ = Throw.IfNull(section);

        return builder.AddFallbackPolicyInternal(policyName, provider, section, null);
    }

    /// <summary>
    /// Adds a fallback policy to a pipeline.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <param name="builder">The policy pipeline builder.</param>
    /// <param name="policyName">The policy name.</param>
    /// <param name="provider">The task performed in the fallback scenario when the initial execution encounters a transient failure.</param>
    /// <param name="section">The configuration that the options will bind against.</param>
    /// <param name="configure">The action that configures the policy options after <paramref name="section"/> is applied.</param>
    /// <returns>Current instance.</returns>
    /// <remarks>
    /// Keep in mind that the <paramref name="configure"/> delegate will override anything that was configured using <paramref name="section"/>.
    /// </remarks>
    public static IResiliencePipelineBuilder<TResult> AddFallbackPolicy<TResult>(
        this IResiliencePipelineBuilder<TResult> builder,
        string policyName,
        FallbackScenarioTaskProvider<TResult> provider,
        IConfigurationSection section,
        Action<FallbackPolicyOptions<TResult>> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(provider);
        _ = Throw.IfNull(section);
        _ = Throw.IfNull(configure);

        return builder.AddFallbackPolicyInternal(policyName, provider, section, configure);
    }

    private static IResiliencePipelineBuilder<TResult> AddFallbackPolicyInternal<TResult>(
        this IResiliencePipelineBuilder<TResult> builder,
        string policyName,
        FallbackScenarioTaskProvider<TResult> provider,
        IConfigurationSection? section,
        Action<FallbackPolicyOptions<TResult>>? configure)
    {
        return builder.AddPolicy<TResult, FallbackPolicyOptions<TResult>, EmptyFallbackPolicyOptionsValidator<TResult>>(
            SupportedPolicies.FallbackPolicy,
            policyName,
            options => options.Configure(section, configure),
            (builder, options) => builder.AddFallbackPolicy(policyName, provider, options));
    }

    private sealed class EmptyFallbackPolicyOptionsValidator<TResult> : IValidateOptions<FallbackPolicyOptions<TResult>>
    {
        public ValidateOptionsResult Validate(string? name, FallbackPolicyOptions<TResult> options) => ValidateOptionsResult.Skip;
    }
}
