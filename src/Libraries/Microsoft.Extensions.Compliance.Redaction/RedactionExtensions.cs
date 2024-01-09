// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Extensions to configure specific redactors.
/// </summary>
public static class RedactionExtensions
{
    /// <summary>
    /// Sets the HMAC redactor to use for a set of data classifications.
    /// </summary>
    /// <param name="builder">The builder to attach the redactor to.</param>
    /// <param name="configure">The configuration function.</param>
    /// <param name="classifications">The data classifications for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/>, <paramref name="configure" />, or <paramref name="classifications" /> is <see langword="null"/>.</exception>
    public static IRedactionBuilder SetHmacRedactor(this IRedactionBuilder builder, Action<HmacRedactorOptions> configure, params DataClassificationSet[] classifications)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);
        _ = Throw.IfNull(classifications);

        _ = builder
                .Services
                .AddOptionsWithValidateOnStart<HmacRedactorOptions, HmacRedactorOptionsValidator>()
                .Configure(configure);

        return builder.SetRedactor<HmacRedactor>(classifications);
    }

    /// <summary>
    /// Sets the HMAC redactor to use for a set of data classifications.
    /// </summary>
    /// <param name="builder">The builder to attach the redactor to.</param>
    /// <param name="section">The configuration section.</param>
    /// <param name="classifications">The data classifications for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/>, <paramref name="section" />, or <paramref name="classifications" /> is <see langword="null"/>.</exception>
    public static IRedactionBuilder SetHmacRedactor(this IRedactionBuilder builder, IConfigurationSection section, params DataClassificationSet[] classifications)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);
        _ = Throw.IfNull(classifications);

        _ = builder
                .Services.AddOptionsWithValidateOnStart<HmacRedactorOptions, HmacRedactorOptionsValidator>()
                .Services.Configure<HmacRedactorOptions>(section);

        return builder.SetRedactor<HmacRedactor>(classifications);
    }
}
