// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Telemetry.Metering;

/// <summary>
/// Extensions to control metering integration.
/// </summary>
public static class MeteringExtensions
{
    /// <summary>
    /// Registers <see cref="T:Microsoft.Extensions.Telemetry.Metering.Meter`1" /> to a dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to register metering into.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection RegisterMetering(this IServiceCollection services);
}
