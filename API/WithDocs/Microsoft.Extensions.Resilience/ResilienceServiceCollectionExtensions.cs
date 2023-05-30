// Assembly 'Microsoft.Extensions.Resilience'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// Extension class for the Service Collection DI container.
/// </summary>
[Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class ResilienceServiceCollectionExtensions
{
    /// <summary>
    /// Adds resilience enrichers.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns>The input <paramref name="services" />.</returns>
    /// <remarks>
    /// This method adds additional dimensions on top of the default ones that are built-in to the Polly library. These include:
    /// <list type="bullet">
    /// <item>
    /// Exception enrichment based on <see cref="T:Microsoft.Extensions.Diagnostics.ExceptionSummarization.IExceptionSummarizer" />.
    /// </item>
    /// <item>
    /// Result enrichment based on <see cref="M:Microsoft.Extensions.Resilience.ResilienceServiceCollectionExtensions.ConfigureFailureResultContext``1(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Func{``0,Microsoft.Extensions.Resilience.FailureResultContext})" /> and <see cref="T:Microsoft.Extensions.Resilience.FailureResultContext" />.
    /// </item>
    /// <item>
    /// Request metadata enrichment based on <see cref="T:Microsoft.Extensions.Http.Telemetry.RequestMetadata" />.
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddResilienceEnrichment(this IServiceCollection services);

    /// <summary>
    /// Configures the failure result dimensions.
    /// </summary>
    /// <typeparam name="TResult">The type of the policy result.</typeparam>
    /// <param name="services">The services.</param>
    /// <param name="configure">The configure result dimensions.</param>
    /// <returns>The input <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="services" /> is <see langword="null" /> or <paramref name="configure" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection ConfigureFailureResultContext<TResult>(this IServiceCollection services, Func<TResult, FailureResultContext> configure);
}
