// Assembly 'Microsoft.Extensions.Diagnostics.ExceptionSummarization'

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

/// <summary>
/// Controls exception summarization.
/// </summary>
public static class ExceptionSummarizationExtensions
{
    /// <summary>
    /// Registers an exception summarizer into a dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to add the summarizer to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddExceptionSummarizer(this IServiceCollection services);

    /// <summary>
    /// Registers an exception summarizer into a dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to add the summarizer to.</param>
    /// <param name="configure">Delegates that configures the set of registered summary providers.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> or <paramref name="configure" /> are <see langword="null" />.</exception>
    public static IServiceCollection AddExceptionSummarizer(this IServiceCollection services, Action<IExceptionSummarizationBuilder> configure);

    /// <summary>
    /// Registers a summary provider that handles <see cref="T:System.OperationCanceledException" />, <see cref="T:System.Net.WebException" />, and <see cref="T:System.Net.Sockets.SocketException" /> .
    /// </summary>
    /// <param name="builder">The builder to attach the provider to.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IExceptionSummarizationBuilder AddHttpProvider(this IExceptionSummarizationBuilder builder);
}
