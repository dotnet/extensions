// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.AI.Evaluation.Integration.Tests;

public class Settings
{
    public bool Configured { get; }
    public string DeploymentName { get; }
    public string ModelName { get; }
    public string Endpoint { get; }
    public string StorageRootPath { get; }

    public Settings(IConfiguration config)
    {
#pragma warning disable CA2208 // Pass correct parameter name for ArgumentNullException.
        Configured = config.GetValue<bool>("Configured", false);

        DeploymentName =
            config.GetValue<string>("DeploymentName") ??
            throw new ArgumentNullException(nameof(DeploymentName));

        ModelName =
            config.GetValue<string>("ModelName") ??
            throw new ArgumentNullException(nameof(ModelName));

        Endpoint =
            config.GetValue<string>("Endpoint") ??
            throw new ArgumentNullException(nameof(Endpoint));

        StorageRootPath =
            config.GetValue<string>("StorageRootPath") ??
            throw new ArgumentNullException(nameof(StorageRootPath));
#pragma warning restore CA2208
    }

    private static Settings? _currentSettings;

    public static Settings Current
    {
        get
        {
            _currentSettings ??= GetCurrentSettings();
            return _currentSettings;
        }
    }

    private static Settings GetCurrentSettings()
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        return new Settings(config);
    }
}
