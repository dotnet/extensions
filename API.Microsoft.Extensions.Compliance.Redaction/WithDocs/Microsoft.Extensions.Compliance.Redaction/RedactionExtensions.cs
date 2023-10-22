// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Extensions to configure specific redactors.
/// </summary>
public static class RedactionExtensions
{
    /// <summary>
    /// Sets the HMAC redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactor to.</param>
    /// <param name="configure">Configuration function.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" />, <paramref name="configure" /> or <paramref name="classifications" /> are <see langword="null" />.</exception>
    [Experimental("EXTEXP0002", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IRedactionBuilder SetHmacRedactor(this IRedactionBuilder builder, Action<HmacRedactorOptions> configure, params DataClassificationSet[] classifications);

    /// <summary>
    /// Sets the HMAC redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactor to.</param>
    /// <param name="section">Configuration section.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" />, <paramref name="section" /> or <paramref name="classifications" /> are <see langword="null" />.</exception>
    [Experimental("EXTEXP0002", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IRedactionBuilder SetHmacRedactor(this IRedactionBuilder builder, IConfigurationSection section, params DataClassificationSet[] classifications);
}
