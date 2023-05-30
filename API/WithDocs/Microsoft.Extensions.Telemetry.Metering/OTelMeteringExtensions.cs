// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Metrics;

namespace Microsoft.Extensions.Telemetry.Metering;

/// <summary>
/// Metering extensions for OpenTelemetry based metrics.
/// </summary>
[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class OTelMeteringExtensions
{
    /// <summary>
    /// Extension to configure metering.
    /// </summary>
    /// <param name="builder"><see cref="T:OpenTelemetry.Metrics.MeterProviderBuilder" /> instance.</param>
    /// <returns>Returns <see cref="T:OpenTelemetry.Metrics.MeterProviderBuilder" /> for chaining.</returns>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static MeterProviderBuilder AddMetering(this MeterProviderBuilder builder);

    /// <summary>
    /// Extension to configure metering.
    /// </summary>
    /// <param name="builder"><see cref="T:OpenTelemetry.Metrics.MeterProviderBuilder" /> instance.</param>
    /// <param name="configurationSection">Configuration section that contains <see cref="T:Microsoft.Extensions.Telemetry.Metering.MeteringOptions" />.</param>
    /// <returns>Returns <see cref="T:OpenTelemetry.Metrics.MeterProviderBuilder" /> for chaining.</returns>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static MeterProviderBuilder AddMetering(this MeterProviderBuilder builder, IConfigurationSection configurationSection);

    /// <summary>
    /// Extension to configure metering.
    /// </summary>
    /// <param name="builder"><see cref="T:OpenTelemetry.Metrics.MeterProviderBuilder" /> instance.</param>
    /// <param name="configure">The <see cref="T:Microsoft.Extensions.Telemetry.Metering.MeteringOptions" /> configuration delegate.</param>
    /// <returns>Returns <see cref="T:OpenTelemetry.Metrics.MeterProviderBuilder" /> for chaining.</returns>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static MeterProviderBuilder AddMetering(this MeterProviderBuilder builder, Action<MeteringOptions> configure);
}
