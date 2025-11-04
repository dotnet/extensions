// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Microsoft.Extensions.AI.Evaluation.Console.Telemetry;

// Note: The below code is based on the code in the following file in the dotnet CLI:
// https://github.com/dotnet/sdk/blob/main/src/Cli/dotnet/Telemetry/DevDeviceIDGetter.cs.
//
// The logic below should be kept in sync with the code linked above to ensure that the device ID remains consistent
// across tools.

internal sealed class DeviceIdHelper
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\DeveloperTools";
    private const string RegistryValueName = "deviceid";
    private const string CacheFileName = "deviceid";

    private static string? _deviceId;

    private readonly ILogger _logger;

    internal DeviceIdHelper(ILogger logger)
    {
        _logger = logger;
    }

    internal string GetDeviceId()
    {
        string? deviceId = GetCachedDeviceId();

        if (string.IsNullOrWhiteSpace(deviceId))
        {
#pragma warning disable CA1308 // Normalize strings to uppercase.
            // The DevDeviceId must follow the format specified below.
            // 1. The value is a randomly generated Guid/ UUID.
            // 2. The value follows the 8-4-4-4-12 format (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).
            // 3. The value shall be all lowercase and only contain hyphens. No braces or brackets.
            deviceId = Guid.NewGuid().ToString("D").ToLowerInvariant();
#pragma warning restore CA1308

            try
            {
                CacheDeviceId(deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache device ID.");

                // If caching fails, return empty string to avoid reporting a non-cached id.
                deviceId = string.Empty;
            }
        }

        return deviceId;
    }

    private static string? GetCachedDeviceId()
    {
        if (_deviceId is not null)
        {
            return _deviceId;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using RegistryKey? key = baseKey.OpenSubKey(RegistryKeyPath);
            _deviceId = key?.GetValue(RegistryValueName) as string;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string cacheFileDirectoryPath = GetCacheFileDirectoryPathForLinux();
            ReadCacheFile(cacheFileDirectoryPath);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            string cacheFileDirectoryPath = GetCacheFileDirectoryPathForMacOS();
            ReadCacheFile(cacheFileDirectoryPath);
        }

        return _deviceId;

        static void ReadCacheFile(string cacheFileDirectoryPath)
        {
            string cacheFilePath = Path.Combine(cacheFileDirectoryPath, CacheFileName);
            if (File.Exists(cacheFilePath))
            {
                _deviceId = File.ReadAllText(cacheFilePath);
            }
        }
    }

    private static void CacheDeviceId(string deviceId)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using RegistryKey key = baseKey.CreateSubKey(RegistryKeyPath);
            key.SetValue(RegistryValueName, deviceId);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string cacheFileDirectoryPath = GetCacheFileDirectoryPathForLinux();
            WriteCacheFile(cacheFileDirectoryPath, deviceId);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            string cacheFileDirectoryPath = GetCacheFileDirectoryPathForMacOS();
            WriteCacheFile(cacheFileDirectoryPath, deviceId);
        }

        _deviceId = deviceId;

        static void WriteCacheFile(string cacheFileDirectoryPath, string deviceId)
        {
            _ = Directory.CreateDirectory(cacheFileDirectoryPath);
            string cacheFilePath = Path.Combine(cacheFileDirectoryPath, CacheFileName);
            File.WriteAllText(cacheFilePath, deviceId);
        }
    }

    private static string GetCacheFileDirectoryPathForLinux()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            throw new InvalidOperationException();
        }

        string cacheFileDirectoryPath;
        string? xdgCacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");

        if (string.IsNullOrWhiteSpace(xdgCacheHome))
        {
            string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            cacheFileDirectoryPath = Path.Combine(userProfilePath, ".cache");
        }
        else
        {
            cacheFileDirectoryPath = Path.Combine(xdgCacheHome, "Microsoft", "DeveloperTools");
        }

        return cacheFileDirectoryPath;
    }

    private static string GetCacheFileDirectoryPathForMacOS()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            throw new InvalidOperationException();
        }

        string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string cacheFileDirectoryPath =
            Path.Combine(userProfilePath, "Library", "Application Support", "Microsoft", "DeveloperTools");

        return cacheFileDirectoryPath;
    }
}
