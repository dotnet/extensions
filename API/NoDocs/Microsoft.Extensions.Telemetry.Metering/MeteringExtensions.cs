// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Telemetry.Metering;

public static class MeteringExtensions
{
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection RegisterMetering(this IServiceCollection services);
}
