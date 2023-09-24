// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System;
using Microsoft.Extensions.Compliance.Testing;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions that allow registering a fake redactor in the application.
/// </summary>
public static class FakeRedactionServiceCollectionExtensions
{
    /// <summary>
    /// Registers the fake redactor provider that always returns fake redactor instances.
    /// </summary>
    /// <param name="services">Container used to register fake redaction classes.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddFakeRedaction(this IServiceCollection services);

    /// <summary>
    /// Registers the fake redactor provider that always returns fake redactor instances.
    /// </summary>
    /// <param name="services">Container used to register fake redaction classes.</param>
    /// <param name="configure">Configures fake redactor.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> or <paramref name="configure" />&gt; are <see langword="null" />.</exception>
    public static IServiceCollection AddFakeRedaction(this IServiceCollection services, Action<FakeRedactorOptions> configure);
}
