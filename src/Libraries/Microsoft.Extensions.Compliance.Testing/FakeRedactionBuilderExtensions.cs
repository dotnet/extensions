// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Extensions that allow registering a fake redactor in the application.
/// </summary>
public static class FakeRedactionBuilderExtensions
{
    /// <summary>
    /// Sets the fake redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactor to.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IRedactionBuilder SetFakeRedactor(this IRedactionBuilder builder, params DataClassificationSet[] classifications)
    {
        _ = Throw.IfNull(builder);

        builder.Services.TryAddSingleton<FakeRedactionCollector>();

        return builder.SetRedactor<FakeRedactor>(classifications);
    }

    /// <summary>
    /// Sets the fake redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactor to.</param>
    /// <param name="configure">Configuration function.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IRedactionBuilder SetFakeRedactor(this IRedactionBuilder builder, Action<FakeRedactorOptions> configure, params DataClassificationSet[] classifications)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        builder
            .Services.AddOptionsWithValidateOnStart<FakeRedactorOptions, FakeRedactorOptionsAutoValidator>()
            .Services.AddOptionsWithValidateOnStart<FakeRedactorOptions, FakeRedactorOptionsCustomValidator>()
            .Configure(configure)
            .Services.TryAddSingleton<FakeRedactionCollector>();

        return builder.SetRedactor<FakeRedactor>(classifications);
    }

    /// <summary>
    /// Sets the fake redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactor to.</param>
    /// <param name="section">Configuration section.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="section"/> is <see langword="null"/>.</exception>
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "The type is FakeRedactorOptions and we know it.")]
    public static IRedactionBuilder SetFakeRedactor(this IRedactionBuilder builder, IConfigurationSection section, params DataClassificationSet[] classifications)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        builder
            .Services.AddOptionsWithValidateOnStart<FakeRedactorOptions, FakeRedactorOptionsAutoValidator>()
            .Services.AddOptionsWithValidateOnStart<FakeRedactorOptions, FakeRedactorOptionsCustomValidator>()
            .Services.Configure<FakeRedactorOptions>(section)
            .TryAddSingleton<FakeRedactionCollector>();

        return builder.SetRedactor<FakeRedactor>(classifications);
    }
}
