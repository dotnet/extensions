// Assembly 'Microsoft.Extensions.Diagnostics.ExceptionSummarization'

using System;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to register exception summarization.
/// </summary>
public static class ExceptionSummarizationServiceCollectionExtensions
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
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddExceptionSummarizer(this IServiceCollection services, Action<IExceptionSummarizationBuilder> configure);
}
