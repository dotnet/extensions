// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AmbientMetadata.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Configuration;

/// <summary>
/// Extensions for Azure Virtual Machine metadata types.
/// </summary>
public static class AzureVmMetadataConfigurationBuilderExtensions
{
    /// <summary>
    /// Registers configuration provider for Azure applications.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="sectionName">Section name to save configuration into. Default set to "ambientmetadata:azurevm".</param>
    /// <returns>The input configuration builder for call chaining.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="builder"/> or <paramref name="sectionName"/> is <see langword="null" />.</exception>
    public static IConfigurationBuilder AddAzureVmMetadata(this IConfigurationBuilder builder, string sectionName = Constants.DefaultSectionName)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrWhitespace(sectionName);

        return builder.Add(new AzureVmMetadataSource(new AzureVmMetadataProvider(), sectionName));
    }
}
