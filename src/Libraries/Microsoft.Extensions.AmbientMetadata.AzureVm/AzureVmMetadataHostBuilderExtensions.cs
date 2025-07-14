// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AmbientMetadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extensions for Azure Virtual Machine metadata types.
/// </summary>
public static class AzureVmMetadataHostBuilderExtensions
{
    /// <summary>
    /// Registers configuration provider for applications running on Azure virtual machines and binds a model object onto the configuration.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="sectionName">Section name to bind configuration from. Default set to "ambientmetadata:azurevm".</param>
    /// <returns>The input host builder for call chaining.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="builder"/> or <paramref name="sectionName"/> is <see langword="null" />.</exception>
    public static IHostBuilder UseAzureVmMetadata(this IHostBuilder builder, string sectionName = Constants.DefaultSectionName)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrWhitespace(sectionName);

        _ = builder
            .ConfigureHostConfiguration(builder =>
                builder.AddAzureVmMetadata(sectionName))
            .ConfigureServices((hostBuilderContext, serviceCollection) =>
                serviceCollection.AddAzureVmMetadata(hostBuilderContext.Configuration.GetSection(sectionName)));

        return builder;
    }

    /// <summary>
    /// Registers configuration provider for applications running on Azure virtual machines and binds a model object onto the configuration.
    /// </summary>
    /// <typeparam name="TBuilder"><see cref="IHostApplicationBuilder"/>.</typeparam>
    /// <param name="builder">The host builder.</param>
    /// <param name="sectionName">Section name to bind configuration from. Default set to "ambientmetadata:azurevm".</param>
    /// <returns>The input host builder for call chaining.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="builder"/> or <paramref name="sectionName"/> is <see langword="null" />.</exception>
    public static TBuilder UseAzureVmMetadata<TBuilder>(this TBuilder builder, string sectionName = Constants.DefaultSectionName)
        where TBuilder : IHostApplicationBuilder
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrWhitespace(sectionName);

        _ = builder.Configuration.AddAzureVmMetadata(sectionName);
        _ = builder.Services.AddAzureVmMetadata(builder.Configuration.GetSection(sectionName));

        return builder;
    }
}
