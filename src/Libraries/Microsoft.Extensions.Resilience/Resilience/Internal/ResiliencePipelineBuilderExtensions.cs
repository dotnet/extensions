// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.Internal;

/// <summary>
/// Pub-internal extension methods for the <see cref="IResiliencePipelineBuilder{TResult}"/>.
/// </summary>
/// <remarks>Do not use this class directly, it's reserved for internal use and can change at any time.</remarks>
[Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
internal static class ResiliencePipelineBuilderExtensions
{
    /// <summary>
    /// Adds a supported policy to a pipeline using the specified options.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <typeparam name="TOptions">The type of policy options.</typeparam>
    /// <typeparam name="TOptionsValidator">Validator that validates <typeparamref name="TOptions"/>.</typeparam>
    /// <param name="builder">The policy pipeline builder.</param>
    /// <param name="policyType">The policy type.</param>
    /// <param name="policyName">The policy name that will be included in the options name.</param>
    /// <param name="configureOptions">The configure options delegate.</param>
    /// <param name="configurePipeline">The configure pipeline delegate.</param>
    /// <returns>Current instance.</returns>
    public static IResiliencePipelineBuilder<TResult> AddPolicy<TResult,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TOptionsValidator>(
        this IResiliencePipelineBuilder<TResult> builder,
        SupportedPolicies policyType,
        string policyName,
        Action<OptionsBuilder<TOptions>> configureOptions,
        Action<Internal.IPolicyPipelineBuilder<TResult>, TOptions> configurePipeline)
            where TOptions : class, new()
            where TOptionsValidator : class, IValidateOptions<TOptions>
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(configureOptions);
        _ = Throw.IfNull(configurePipeline);

        return builder.AddPolicy<TResult, TOptions, TOptionsValidator>(
            policyType.GetPolicyOptionsName(builder.PipelineName, policyName),
            configureOptions,
            (b, o, _) => configurePipeline(b, o));
    }

    /// <summary>
    /// Adds a supported policy to a pipeline using the specified options.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <typeparam name="TOptions">The type of policy options.</typeparam>
    /// <typeparam name="TOptionsValidator">Validator that validates <typeparamref name="TOptions"/>.</typeparam>
    /// <param name="builder">The policy pipeline builder.</param>
    /// <param name="optionsName">The options name.</param>
    /// <param name="configureOptions">The configure options delegate.</param>
    /// <param name="configurePipeline">The configure pipeline delegate.</param>
    /// <returns>Current instance.</returns>
    public static IResiliencePipelineBuilder<TResult> AddPolicy<TResult,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TOptionsValidator>(
        this IResiliencePipelineBuilder<TResult> builder,
        string optionsName,
        Action<OptionsBuilder<TOptions>> configureOptions,
        Action<IPolicyPipelineBuilder<TResult>, TOptions, IServiceProvider> configurePipeline)
            where TOptions : class, new()
            where TOptionsValidator : class, IValidateOptions<TOptions>
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(optionsName);
        _ = Throw.IfNull(configureOptions);
        _ = Throw.IfNull(configurePipeline);

        var services = builder.Services;
        var optionsBuilder = services.AddValidatedOptions<TOptions, TOptionsValidator>(optionsName);

        configureOptions(optionsBuilder);
        return builder
            .ConfigureDynamicPolicy<TResult, TOptions>(optionsName)
            .AddPolicy((policyBuilder, serviceProvider) =>
            {
                var optionsListenersHandler = serviceProvider.GetRequiredService<IOnChangeListenersHandler>();
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<TOptions>>();
                var options = optionsMonitor.Get(optionsName);

                _ = optionsListenersHandler.TryCaptureOnChange<TOptions>(optionsName);
                configurePipeline(policyBuilder, options, serviceProvider);
            });
    }

    /// <summary>
    /// Adds a policy.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <param name="builder">The policy pipeline builder.</param>
    /// <param name="configure">The action that configures the pipeline builder instance.</param>
    /// <returns>Current instance.</returns>
    public static IResiliencePipelineBuilder<TResult> AddPolicy<TResult>(
        this IResiliencePipelineBuilder<TResult> builder,
        Action<Internal.IPolicyPipelineBuilder<TResult>, IServiceProvider> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services
            .AddOptions<ResiliencePipelineFactoryOptions<TResult>>(builder.PipelineName)
            .Configure<IServiceProvider>((options, serviceProvider) =>
            {
                options.BuilderActions.Add((builder) => configure(builder, serviceProvider));
            });

        return builder;
    }

    /// <summary>
    /// Ensures the policy with options named <paramref name="policyOptionsName"/> will support dynamic configurations,
    /// triggering pipeline reloads on changes.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <typeparam name="TOptions">The type of policy options.</typeparam>
    /// <param name="builder">The policy pipeline builder.</param>
    /// <param name="policyOptionsName">The name of the options of the individual policy added.</param>
    /// <returns>Current instance.</returns>
    private static IResiliencePipelineBuilder<TResult> ConfigureDynamicPolicy<TResult,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(
        this IResiliencePipelineBuilder<TResult> builder,
        string policyOptionsName)
        where TOptions : class, new()
    {
        _ = builder.Services
            .AddOptions<ResiliencePipelineFactoryTokenSourceOptions<TResult>>(builder.PipelineName)
            .Configure<IServiceProvider>((options, sp) =>
            {
                var source = sp.GetServices<IOptionsChangeTokenSource<TOptions>>()
                    .FirstOrDefault(source => source.Name == policyOptionsName);

                if (source != null)
                {
                    options.ChangeTokenSources.Add(() => source.GetChangeToken());
                }
            });

        return builder;
    }
}
