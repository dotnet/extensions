// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AmbientMetadata;

/// <summary>
/// Provides virtual configuration source for service metadata information.
/// </summary>
internal sealed class ApplicationMetadataSource : IConfigurationSource
{
    private readonly IHostEnvironment _hostEnvironment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationMetadataSource"/> class.
    /// </summary>
    /// <param name="hostEnvironment">An instance of <see cref="IHostEnvironment"/>.</param>
    /// <param name="sectionName">Section name to be used in configuration.</param>
    /// <exception cref="ArgumentNullException"><paramref name="hostEnvironment"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sectionName"/> is either <see langword="null"/>, empty or whitespace.</exception>
    public ApplicationMetadataSource(IHostEnvironment hostEnvironment, string sectionName)
    {
        _hostEnvironment = Throw.IfNull(hostEnvironment);
        SectionName = Throw.IfNullOrWhitespace(sectionName);
    }

    /// <summary>
    /// Gets configuration section name.
    /// </summary>
    public string SectionName { get; }

    /// <summary>
    /// Builds an <see cref="IConfigurationProvider"/> for the source.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder" /> to add to.</param>
    /// <returns>The configuration provider.</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var provider = new MemoryConfigurationProvider(new())
        {
            { $"{SectionName}:{nameof(ApplicationMetadata.EnvironmentName)}", _hostEnvironment.EnvironmentName },
            { $"{SectionName}:{nameof(ApplicationMetadata.ApplicationName)}", _hostEnvironment.ApplicationName },
        };

        return provider;
    }
}
