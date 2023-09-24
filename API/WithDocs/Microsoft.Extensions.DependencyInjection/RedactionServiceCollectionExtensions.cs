// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System;
using Microsoft.Extensions.Compliance.Redaction;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to register redaction functionality.
/// </summary>
public static class RedactionServiceCollectionExtensions
{
    /// <summary>
    /// Registers an implementation of <see cref="T:Microsoft.Extensions.Compliance.Redaction.IRedactorProvider" /> in the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">Instance of <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> used to configure redaction.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRedaction(this IServiceCollection services);

    /// <summary>
    /// Registers an implementation of <see cref="T:Microsoft.Extensions.Compliance.Redaction.IRedactorProvider" /> in the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> and configures available redactors.
    /// </summary>
    /// <param name="services">Instance of <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> used to configure redaction.</param>
    /// <param name="configure">Configuration function for <see cref="T:Microsoft.Extensions.Compliance.Redaction.IRedactionBuilder" />.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="configure" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRedaction(this IServiceCollection services, Action<IRedactionBuilder> configure);
}
