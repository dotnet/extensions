// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.AmbientMetadata;

/// <summary>
/// Application-level metadata model.
/// </summary>
public class ApplicationMetadata
{
    /// <summary>
    /// Gets or sets a value that represents the deployment ring from where the application is running.
    /// </summary>
    public string? DeploymentRing { get; set; }

    /// <summary>
    /// Gets or sets a value that represents the application's build version.
    /// </summary>
    public string? BuildVersion { get; set; }

    /// <summary>
    /// Gets or sets a value that represents the application's name.
    /// </summary>
    [Required]
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value that represents the application's environment name, such as Development, Staging, Production.
    /// </summary>
    [Required]
    public string EnvironmentName { get; set; } = string.Empty;
}
