// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Metrics;

public static class MetricsExtensions
{
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection RegisterMetrics(this IServiceCollection services);
}
