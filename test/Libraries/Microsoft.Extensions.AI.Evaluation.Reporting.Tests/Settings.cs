// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Tests;

public class Settings
{
    public bool Configured { get; }
    public string StorageAccountEndpoint { get; }
    public string StorageContainerName { get; }

    public Settings(IConfiguration config)
    {
#pragma warning disable CA2208 // Pass correct parameter name for ArgumentNullException.
        Configured = config.GetValue<bool>("Configured", false);

        StorageAccountEndpoint =
            config.GetValue<string>("StorageAccountEndpoint")
            ?? throw new ArgumentNullException(nameof(StorageAccountEndpoint));

        StorageContainerName =
            config.GetValue<string>("StorageContainerName")
            ?? throw new ArgumentNullException(nameof(StorageContainerName));
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
