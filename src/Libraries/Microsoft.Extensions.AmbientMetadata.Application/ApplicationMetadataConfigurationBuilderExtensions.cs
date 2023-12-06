// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Hosting;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Configuration;

/// <summary>
/// Extensions for application metadata.
/// </summary>
public static class ApplicationMetadataConfigurationBuilderExtensions
{
    private const string DefaultSectionName = "ambientmetadata:application";

    /// <summary>
    /// Registers a configuration provider for application metadata.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="hostEnvironment">An instance of <see cref="IHostEnvironment" />.</param>
    /// <param name="sectionName">Section name to save configuration into. Default set to "ambientmetadata:application".</param>
    /// <returns>The value of <paramref name="builder"/>>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="hostEnvironment"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sectionName"/> is either <see langword="null"/>, empty, or whitespace.</exception>
    public static IConfigurationBuilder AddApplicationMetadata(this IConfigurationBuilder builder, IHostEnvironment hostEnvironment, string sectionName = DefaultSectionName)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(hostEnvironment);
        _ = Throw.IfNullOrWhitespace(sectionName);

        return builder.Add(new ApplicationMetadataSource(hostEnvironment, sectionName));
    }
}
