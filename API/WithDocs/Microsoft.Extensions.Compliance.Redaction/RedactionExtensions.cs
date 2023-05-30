// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Add redaction to the application.
/// </summary>
public static class RedactionExtensions
{
    /// <summary>
    /// Registers redaction in the application.
    /// </summary>
    /// <param name="builder"><see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> instance.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="builder" /> is <see langword="null" />.</exception>
    public static IHostBuilder ConfigureRedaction(this IHostBuilder builder);

    /// <summary>
    /// Registers redaction in the application.
    /// </summary>
    /// <param name="builder"><see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> instance.</param>
    /// <param name="configure">Configuration for <see cref="T:Microsoft.Extensions.Compliance.Redaction.IRedactionBuilder" />.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="builder" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="configure" /> is <see langword="null" />.</exception>
    public static IHostBuilder ConfigureRedaction(this IHostBuilder builder, Action<HostBuilderContext, IRedactionBuilder> configure);

    /// <summary>
    /// Registers redaction in the application.
    /// </summary>
    /// <param name="builder"><see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> instance.</param>
    /// <param name="configure">Configuration for <see cref="T:Microsoft.Extensions.Compliance.Redaction.IRedactionBuilder" />.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="builder" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="configure" /> is <see langword="null" />.</exception>
    public static IHostBuilder ConfigureRedaction(this IHostBuilder builder, Action<IRedactionBuilder> configure);

    /// <summary>
    /// Registers an implementation of <see cref="T:Microsoft.Extensions.Compliance.Redaction.IRedactorProvider" /> in the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">Instance of <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> used to configure redaction.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRedaction(this IServiceCollection services);

    /// <summary>
    /// Registers an implementation of <see cref="T:Microsoft.Extensions.Compliance.Redaction.IRedactorProvider" /> in the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> and configures available redactors.
    /// </summary>
    /// <param name="services">Instance of <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> used to configure redaction.</param>
    /// <param name="configure">Configuration function for <see cref="T:Microsoft.Extensions.Compliance.Redaction.IRedactionBuilder" />.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="services" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="configure" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRedaction(this IServiceCollection services, Action<IRedactionBuilder> configure);

    /// <summary>
    /// Sets the xxHash3 redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactor to.</param>
    /// <param name="configure">Configuration function.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">If <paramref name="builder" />, <paramref name="configure" /> or <paramref name="classifications" /> are <see langword="null" />.</exception>
    public static IRedactionBuilder SetXXHash3Redactor(this IRedactionBuilder builder, Action<XXHash3RedactorOptions> configure, params DataClassification[] classifications);

    /// <summary>
    /// Sets the xxHash3 redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactor to.</param>
    /// <param name="section">Configuration section.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">If <paramref name="builder" />, <paramref name="section" /> or <paramref name="classifications" /> are <see langword="null" />.</exception>
    public static IRedactionBuilder SetXXHash3Redactor(this IRedactionBuilder builder, IConfigurationSection section, params DataClassification[] classifications);
}
