// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Extensions that allow registering a fake redactor in the application.
/// </summary>
public static class FakeRedactionExtensions
{
    /// <summary>
    /// Sets the fake redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactorr to.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="builder" /> is <see langword="null" />.</exception>
    public static IRedactionBuilder SetFakeRedactor(this IRedactionBuilder builder, params DataClassification[] classifications);

    /// <summary>
    /// Sets the fake redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactorr to.</param>
    /// <param name="configure">Configuration function.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="builder" /> or <paramref name="configure" /> are <see langword="null" />.</exception>
    public static IRedactionBuilder SetFakeRedactor(this IRedactionBuilder builder, Action<FakeRedactorOptions> configure, params DataClassification[] classifications);

    /// <summary>
    /// Sets the fake redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactorr to.</param>
    /// <param name="section">Configuration section.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="builder" /> or <paramref name="section" /> are <see langword="null" />.</exception>
    public static IRedactionBuilder SetFakeRedactor(this IRedactionBuilder builder, IConfigurationSection section, params DataClassification[] classifications);

    /// <summary>
    /// Registers the fake redactor provider that always returns fake redactor instances.
    /// </summary>
    /// <param name="services">Container used to register fake redaction classes.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddFakeRedaction(this IServiceCollection services);

    /// <summary>
    /// Registers the fake redactor provider that always returns fake redactor instances.
    /// </summary>
    /// <param name="services">Container used to register fake redaction classes.</param>
    /// <param name="configure">Configures fake redactor.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="services" /> or <paramref name="configure" />&gt; are <see langword="null" />.</exception>
    public static IServiceCollection AddFakeRedaction(this IServiceCollection services, Action<FakeRedactorOptions> configure);

    /// <summary>
    /// Gets the fake redacton collector instance from the dependency injection container.
    /// </summary>
    /// <param name="serviceProvider">Container used to obtain collector instance.</param>
    /// <returns>Obtained collector.</returns>
    /// <exception cref="T:System.InvalidOperationException">When collector is not in the container.</exception>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="serviceProvider" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <see cref="T:Microsoft.Extensions.Compliance.Testing.FakeRedactionCollector" /> should be registered and used only with fake redaction implementation.
    /// </remarks>
    public static FakeRedactionCollector GetFakeRedactionCollector(this IServiceProvider serviceProvider);
}
