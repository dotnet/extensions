// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience;

public static partial class ResiliencePipelineBuilderExtensions
{
    /// <summary>
    /// Adds a hedging policy with default options to a pipeline.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <param name="builder">The policy pipeline builder.</param>
    /// <param name="policyName">The policy name.</param>
    /// <param name="provider">The hedged task provider.</param>
    /// <returns>Current instance.</returns>
    public static IResiliencePipelineBuilder<TResult> AddHedgingPolicy<TResult>(
        this IResiliencePipelineBuilder<TResult> builder,
        string policyName,
        HedgedTaskProvider<TResult> provider)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(provider);

        return builder.AddHedgingPolicyInternal(policyName, provider, null, null);
    }

    /// <summary>
    /// Adds a hedging policy to a pipeline.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <param name="builder">The policy pipeline builder.</param>
    /// <param name="policyName">The policy name.</param>
    /// <param name="provider">The hedged task provider.</param>
    /// <param name="configure">The action that configures the default policy options.</param>
    /// <returns>Current instance.</returns>
    public static IResiliencePipelineBuilder<TResult> AddHedgingPolicy<TResult>(
        this IResiliencePipelineBuilder<TResult> builder,
        string policyName,
        HedgedTaskProvider<TResult> provider,
        Action<HedgingPolicyOptions<TResult>> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(configure);
        _ = Throw.IfNull(provider);

        return builder.AddHedgingPolicyInternal(policyName, provider, null, configure);
    }

    /// <summary>
    /// Adds a hedging policy to a pipeline.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <param name="builder">The policy pipeline builder.</param>
    /// <param name="policyName">The policy name.</param>
    /// <param name="provider">The hedged task provider.</param>
    /// <param name="section">The configuration that the options will bind against.</param>
    /// <returns>Current instance.</returns>
    public static IResiliencePipelineBuilder<TResult> AddHedgingPolicy<TResult>(
        this IResiliencePipelineBuilder<TResult> builder,
        string policyName,
        HedgedTaskProvider<TResult> provider,
        IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(provider);
        _ = Throw.IfNull(section);

        return builder.AddHedgingPolicyInternal(policyName, provider, section, null);
    }

    /// <summary>
    /// Adds a hedging policy to a pipeline.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <param name="builder">The policy pipeline builder.</param>
    /// <param name="policyName">The policy name.</param>
    /// <param name="provider">The hedged task provider.</param>
    /// <param name="section">The configuration that the options will bind against.</param>
    /// <param name="configure">The action that configures the policy options after <paramref name="section"/> is applied.</param>
    /// <returns>Current instance.</returns>
    /// <remarks>
    /// Keep in mind that the <paramref name="configure"/> delegate will override anything that was configured using <paramref name="section"/>.
    /// </remarks>
    public static IResiliencePipelineBuilder<TResult> AddHedgingPolicy<TResult>(
        this IResiliencePipelineBuilder<TResult> builder,
        string policyName,
        HedgedTaskProvider<TResult> provider,
        IConfigurationSection section,
        Action<HedgingPolicyOptions<TResult>> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(provider);
        _ = Throw.IfNull(section);
        _ = Throw.IfNull(configure);

        return builder.AddHedgingPolicyInternal(policyName, provider, section, configure);
    }

    private static IResiliencePipelineBuilder<TResult> AddHedgingPolicyInternal<TResult>(
        this IResiliencePipelineBuilder<TResult> builder,
        string policyName,
        HedgedTaskProvider<TResult> provider,
        IConfigurationSection? section,
        Action<HedgingPolicyOptions<TResult>>? configure)
    {
        return builder.AddPolicy<TResult, HedgingPolicyOptions<TResult>, HedgingPolicyOptionsValidator<TResult>>(
            SupportedPolicies.HedgingPolicy,
            policyName,
            options => options.Configure(section, configure),
            (builder, options) => builder.AddHedgingPolicy(policyName, provider, options));
    }
}
