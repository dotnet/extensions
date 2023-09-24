// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Extensions to add redactors to a redaction builder.
/// </summary>
public static class RedactionBuilderExtensions
{
    /// <summary>
    /// Sets the xxHash3 redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactor to.</param>
    /// <param name="configure">Configuration function.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" />, <paramref name="configure" /> or <paramref name="classifications" /> are <see langword="null" />.</exception>
    public static IRedactionBuilder SetXxHash3Redactor(this IRedactionBuilder builder, Action<XxHash3RedactorOptions> configure, params DataClassification[] classifications);

    /// <summary>
    /// Sets the xxHash3 redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactor to.</param>
    /// <param name="section">Configuration section.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" />, <paramref name="section" /> or <paramref name="classifications" /> are <see langword="null" />.</exception>
    public static IRedactionBuilder SetXxHash3Redactor(this IRedactionBuilder builder, IConfigurationSection section, params DataClassification[] classifications);
}
