// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.Internal;

/// <summary>
/// Extensions for <see cref="OptionsBuilder{TOptions}"/>.
/// </summary>
[Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
internal static class OptionsBuilderExtensions
{
    /// <summary>
    /// Configures the options based on the <paramref name="section"/> and <paramref name="configureOptions"/>.
    /// </summary>
    /// <typeparam name="T">The options type.</typeparam>
    /// <param name="builder">The options builder instance.</param>
    /// <param name="section">The section. This parameter can be <see langword="null"/>.</param>
    /// <param name="configureOptions">The configure options action. This parameter can be <see langword="null"/>.</param>
    /// <returns>The builder instance.</returns>
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public static OptionsBuilder<T> Configure<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this OptionsBuilder<T> builder,
        IConfigurationSection? section,
        Action<T>? configureOptions)
            where T : class, new()
    {
        _ = Throw.IfNull(builder);

        if (section != null)
        {
            _ = builder.Bind(section);
        }

        if (configureOptions != null)
        {
            _ = builder.Configure(configureOptions);
        }

        return builder;
    }
}
