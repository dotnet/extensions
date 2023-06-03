// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Telemetry.Tracing.Internal;
using Microsoft.Shared.Diagnostics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Telemetry.Tracing;

/// <summary>
/// Extension methods for setting up trace sampling.
/// </summary>
public static class SamplingExtensions
{
    /// <summary>
    /// Adds sampling for traces.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add sampling to.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="builder"/> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddSampling(this TracerProviderBuilder builder)
    {
        _ = Throw.IfNull(builder);

        return builder
            .AddSamplingInternal();
    }

    /// <summary>
    /// Adds sampling for traces.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add sampling to.</param>
    /// <param name="configure">The <see cref="SamplingOptions"/> configuration delegate.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="builder"/> or <paramref name="configure"/> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddSampling(
        this TracerProviderBuilder builder,
        Action<SamplingOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder
            .ConfigureServices(services => services.Configure(configure))
            .AddSamplingInternal();
    }

    /// <summary>
    /// Adds sampling for traces.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add sampling to.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="SamplingOptions"/>.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="builder"/> or <paramref name="section"/> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddSampling(
        this TracerProviderBuilder builder,
        IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        return builder
            .ConfigureServices(services => services.Configure<SamplingOptions>(section))
            .AddSamplingInternal();
    }

    private static TracerProviderBuilder AddSamplingInternal(this TracerProviderBuilder builder)
    {
        return builder.ConfigureServices(services =>
        {
            _ = services.AddValidatedOptions<SamplingOptions, SamplingOptionsAutoValidator>();
            _ = services.AddValidatedOptions<SamplingOptions, SamplingOptionsCustomValidator>();

            _ = services.ConfigureOpenTelemetryTracerProvider((serviceProvider, builder) =>
            {
                SamplingOptions o = serviceProvider.GetRequiredService<IOptions<SamplingOptions>>().Value;

                Sampler sampler = o.SamplerType switch
                {
                    SamplerType.AlwaysOn =>
                        new AlwaysOnSampler(),
                    SamplerType.AlwaysOff =>
                        new AlwaysOffSampler(),
                    SamplerType.TraceIdRatioBased =>
                        new TraceIdRatioBasedSampler(
                            o.TraceIdRatioBasedSamplerOptions!.Probability),
                    SamplerType.ParentBased
                        when o.ParentBasedSamplerOptions!.RootSamplerType == SamplerType.AlwaysOn =>
                        new ParentBasedSampler(
                            new AlwaysOnSampler()),
                    SamplerType.ParentBased
                        when o.ParentBasedSamplerOptions!.RootSamplerType == SamplerType.AlwaysOff =>
                        new ParentBasedSampler(
                            new AlwaysOffSampler()),
                    SamplerType.ParentBased
                        when o.ParentBasedSamplerOptions!.RootSamplerType == SamplerType.TraceIdRatioBased =>
                        new ParentBasedSampler(
                            new TraceIdRatioBasedSampler(
                                o.ParentBasedSamplerOptions.TraceIdRatioBasedSamplerOptions!.Probability)),
                    SamplerType st =>
                        throw new InvalidOperationException($"Invalid sampling configuration for '{st}'.")
                };

                _ = builder.SetSampler(sampler);
            });
        });
    }
}
