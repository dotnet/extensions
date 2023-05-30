// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Metrics;

namespace Microsoft.Extensions.Telemetry.Metering;

[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class OTelMeteringExtensions
{
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static MeterProviderBuilder AddMetering(this MeterProviderBuilder builder);
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static MeterProviderBuilder AddMetering(this MeterProviderBuilder builder, IConfigurationSection configurationSection);
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static MeterProviderBuilder AddMetering(this MeterProviderBuilder builder, Action<MeteringOptions> configure);
}
