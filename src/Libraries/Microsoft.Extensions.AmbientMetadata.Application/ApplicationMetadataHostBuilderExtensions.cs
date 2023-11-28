// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extensions for application metadata.
/// </summary>
public static class ApplicationMetadataHostBuilderExtensions
{
    private const string DefaultSectionName = "ambientmetadata:application";

    /// <summary>
    /// Registers a configuration provider for application metadata and binds a model object onto the configuration.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="sectionName">Section name to bind configuration from. Default set to "ambientmetadata:application".</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sectionName"/> is either <see langword="null"/>, empty, or whitespace.</exception>
    public static IHostBuilder UseApplicationMetadata(this IHostBuilder builder, string sectionName = DefaultSectionName)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrWhitespace(sectionName);

        return builder
            .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) => configurationBuilder.AddApplicationMetadata(hostBuilderContext.HostingEnvironment, sectionName))
            .ConfigureServices((hostBuilderContext, serviceCollection) => serviceCollection.AddApplicationMetadata(hostBuilderContext.Configuration.GetSection(sectionName)));
    }
}
