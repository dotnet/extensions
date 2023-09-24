// Assembly 'Microsoft.Extensions.Resilience'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Resilience;

namespace Microsoft.Extensions.DependencyInjection;

[Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class ResilienceServiceCollectionExtensions
{
    public static IServiceCollection AddResilienceEnrichment(this IServiceCollection services);
    public static IServiceCollection ConfigureFailureResultContext<TResult>(this IServiceCollection services, Func<TResult, FailureResultContext> configure);
}
