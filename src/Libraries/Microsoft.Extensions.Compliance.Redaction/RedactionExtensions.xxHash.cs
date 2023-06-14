// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Redaction;

public static partial class RedactionExtensions
{
    /// <summary>
    /// Sets the xxHash3 redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactor to.</param>
    /// <param name="configure">Configuration function.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/>, <paramref name="configure" /> or <paramref name="classifications" /> are <see langword="null"/>.</exception>
    public static IRedactionBuilder SetXXHash3Redactor(this IRedactionBuilder builder, Action<XXHash3RedactorOptions> configure, params DataClassification[] classifications)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);
        _ = Throw.IfNull(classifications);

        _ = builder
                .Services
                .AddOptions<XXHash3RedactorOptions>()
                .Configure(configure);

        return builder.SetRedactor<XXHash3Redactor>(classifications);
    }

    /// <summary>
    /// Sets the xxHash3 redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactor to.</param>
    /// <param name="section">Configuration section.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/>, <paramref name="section" /> or <paramref name="classifications" /> are <see langword="null"/>.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(XXHash3RedactorOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IRedactionBuilder SetXXHash3Redactor(this IRedactionBuilder builder, IConfigurationSection section, params DataClassification[] classifications)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);
        _ = Throw.IfNull(classifications);

        _ = builder
                .Services.AddOptions<XXHash3RedactorOptions>()
                .Services.Configure<XXHash3RedactorOptions>(section);

        return builder.SetRedactor<XXHash3Redactor>(classifications);
    }
}
