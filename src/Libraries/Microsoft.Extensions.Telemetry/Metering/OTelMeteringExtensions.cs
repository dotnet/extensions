// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;
using OpenTelemetry.Metrics;

namespace Microsoft.Extensions.Telemetry.Metering;

/// <summary>
/// Metering extensions for OpenTelemetry based metrics.
/// </summary>
[Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class OTelMeteringExtensions
{
    /// <summary>
    /// Extension to configure metering.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> instance.</param>
    /// <returns>Returns <see cref="MeterProviderBuilder"/> for chaining.</returns>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static MeterProviderBuilder AddMetering(
        this MeterProviderBuilder builder)
    {
        return builder.AddMetering(_ => { });
    }

    /// <summary>
    /// Extension to configure metering.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> instance.</param>
    /// <param name="configurationSection">Configuration section that contains <see cref="MeteringOptions"/>.</param>
    /// <returns>Returns <see cref="MeterProviderBuilder"/> for chaining.</returns>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static MeterProviderBuilder AddMetering(
        this MeterProviderBuilder builder,
        IConfigurationSection configurationSection)
    {
        _ = Throw.IfNull(builder);

        _ = builder.ConfigureServices(services => services.Configure<MeteringOptions>(configurationSection));
        return builder.AddMetering();
    }

    /// <summary>
    /// Extension to configure metering.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> instance.</param>
    /// <param name="configure">The <see cref="MeteringOptions"/> configuration delegate.</param>
    /// <returns>Returns <see cref="MeterProviderBuilder"/> for chaining.</returns>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static MeterProviderBuilder AddMetering(
        this MeterProviderBuilder builder,
        Action<MeteringOptions> configure)
    {
        _ = Throw.IfNull(builder);

        return builder.ConfigureServices(services =>
            services
                .RegisterMetering()
                .ConfigureOpenTelemetryMeterProvider((sp, meterProviderBuilder) =>
            {
                _ = meterProviderBuilder.AddMeteringInternal(sp.GetOptions<MeteringOptions>(), configure);
            }));
    }

    private static MeterProviderBuilder AddMeteringInternal(
        this MeterProviderBuilder builder,
        MeteringOptions options,
        Action<MeteringOptions>? configure = null)
    {
        configure?.Invoke(options);

        const string Wildcard = "*";
        if (options.MeterState == MeteringState.Enabled)
        {
            _ = builder.AddMeter(Wildcard);
        }
        else if (options.MeterStateOverrides.Count > 0)
        {
            foreach (var meterStateOverride in options.MeterStateOverrides)
            {
                if (meterStateOverride.Value == MeteringState.Enabled)
                {
                    _ = builder.AddMeter($"{meterStateOverride.Key}{Wildcard}");
                }
            }
        }

        return builder
            .SetMaxMetricStreams(options.MaxMetricStreams)
            .SetMaxMetricPointsPerMetricStream(options.MaxMetricPointsPerStream)
            .AddView((instrument) =>
            {
                if (GetMeterState(options, instrument.Meter.Name) == MeteringState.Disabled)
                {
                    return MetricStreamConfiguration.Drop;
                }

                return null;
            });
    }

    private static T GetOptions<T>(this IServiceProvider serviceProvider)
        where T : class, new()
    {
        IOptions<T> options = (IOptions<T>?)serviceProvider.GetService(typeof(IOptions<T>))!;
        return options.Value;
    }

    private static bool IsBetterCategoryMatch(string newCategory, string currentCategory, string typeName)
    {
        if (string.IsNullOrEmpty(newCategory))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(currentCategory) && currentCategory.Length > newCategory.Length)
        {
            return false;
        }

        if (!typeName.StartsWith(newCategory, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static MeteringState GetMeterState(MeteringOptions meteringOptions, string typeName)
    {
        MeteringState meterState = meteringOptions.MeterState;
        string currentCategory = string.Empty;

        foreach (var meterStateOverride in meteringOptions.MeterStateOverrides)
        {
            if (IsBetterCategoryMatch(meterStateOverride.Key, currentCategory, typeName))
            {
                currentCategory = meterStateOverride.Key;
                meterState = meterStateOverride.Value;
            }
        }

        return meterState;
    }
}
