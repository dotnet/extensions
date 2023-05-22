// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Extensions.Telemetry.Internal;

/// <remarks>
/// This is copied from the OpenTelemetry-dotnet repo
/// https://github.com/open-telemetry/opentelemetry-dotnet/blob/952c3b17fc2eaa0622f5f3efd336d4cf103c2813/src/OpenTelemetry/Internal/SelfDiagnosticsConfigParser.cs
/// as the class is internal and not visible to this project. This will be removed from R9 library
/// in one of the two conditions below.
///  - OpenTelemetry-dotnet will make it internalVisible to R9 library.
///  - This class will be added to OpenTelemetry-dotnet project as public.
/// </remarks>
#pragma warning disable R9A013 // Class inherited in tests.
internal class SelfDiagnosticsConfigParser
#pragma warning restore R9A013 // Class inherited in tests.
{
    public const string ConfigFileName = "OTEL_DIAGNOSTICS.json";

    internal const int FileSizeLowerLimit = 1024;  // Lower limit for log file size in KB: 1MB
    internal const int FileSizeUpperLimit = 128 * 1024;  // Upper limit for log file size in KB: 128MB

    // This class is called in SelfDiagnosticsConfigRefresher.UpdateMemoryMappedFileFromConfiguration
    // in both main thread and the worker thread.
    // In theory the variable won't be access at the same time because worker thread first Task.Delay for a few seconds.
    internal byte[]? ConfigBuffer;

    /// <summary>
    /// ConfigBufferSize is the maximum bytes of config file that will be read.
    /// </summary>
    private const int ConfigBufferSize = 4 * 1024;

    private static readonly Regex _logDirectoryRegex = SelfDiagnosticsConfigParserRegex.MakeLogDirectoryRegex();
    private static readonly Regex _fileSizeRegex = SelfDiagnosticsConfigParserRegex.MakeFileSizeRegex();
    private static readonly Regex _logLevelRegex = SelfDiagnosticsConfigParserRegex.MakeLogLevelRegex();

    public bool TryGetConfiguration(out string logDirectory, out int fileSizeInKb, out EventLevel logLevel)
    {
        logDirectory = string.Empty;
        fileSizeInKb = 0;
        logLevel = EventLevel.LogAlways;

        if (!TryReadConfigFile(ConfigFileName, out var configJson))
        {
            return false;
        }

        if (!TryParseLogDirectory(configJson, out logDirectory))
        {
            return false;
        }

        if (!TryParseFileSize(configJson, out fileSizeInKb))
        {
            return false;
        }

        fileSizeInKb = SetFileSizeWithinLimit(fileSizeInKb);

        if (!TryParseLogLevel(configJson, out var logLevelString))
        {
            return false;
        }

        logLevel = (EventLevel)Enum.Parse(typeof(EventLevel), logLevelString);
        return true;

    }

    internal static bool TryParseLogDirectory(string configJson, out string logDirectory)
    {
        var logDirectoryResult = _logDirectoryRegex.Match(configJson);
        logDirectory = logDirectoryResult.Groups["LogDirectory"].Value;
        return logDirectoryResult.Success && !string.IsNullOrWhiteSpace(logDirectory);
    }

    internal static bool TryParseFileSize(string configJson, out int fileSizeInKb)
    {
        fileSizeInKb = 0;
        var fileSizeResult = _fileSizeRegex.Match(configJson);
        return fileSizeResult.Success && int.TryParse(fileSizeResult.Groups["FileSize"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out fileSizeInKb);
    }

    internal static bool TryParseLogLevel(string configJson, out string logLevel)
    {
        var logLevelResult = _logLevelRegex.Match(configJson);
        logLevel = logLevelResult.Groups["LogLevel"].Value;
        return logLevelResult.Success && !string.IsNullOrWhiteSpace(logLevel);
    }

    internal virtual int SetFileSizeWithinLimit(int fileSizeInKb)
    {
        if (fileSizeInKb < FileSizeLowerLimit)
        {
            fileSizeInKb = FileSizeLowerLimit;
        }

        if (fileSizeInKb > FileSizeUpperLimit)
        {
            fileSizeInKb = FileSizeUpperLimit;
        }

        return fileSizeInKb;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance",
        "R9A017:Use asynchronous operations instead of legacy thread blocking code",
        Justification = "Imported from OpenTelemetry-dotnet repo.")]
    internal virtual bool TryReadConfigFile(string configFilePath, out string configJson)
    {
        configJson = string.Empty;

        try
        {
            // First check using current working directory
            if (!File.Exists(configFilePath))
            {
                configFilePath = Path.Combine(AppContext.BaseDirectory, configFilePath);

                // Second check using application base directory
                if (!File.Exists(configFilePath))
                {
                    return false;
                }
            }

            using var file = File.Open(configFilePath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            var buffer = ConfigBuffer;
            if (buffer == null)
            {
                buffer = new byte[ConfigBufferSize]; // Fail silently if OOM
                ConfigBuffer = buffer;
            }

            _ = file.Read(buffer, 0, buffer.Length);

            configJson = Encoding.UTF8.GetString(buffer);
            return true;
        }
#pragma warning disable CA1031 // Do not catch general exception types - this tools is nice-to-have and good if it just works, it should not never throw if anything happens.
        catch (Exception)
        {
            // do nothing on failure to open/read/parse config file
        }
#pragma warning restore CA1031 // Do not catch general exception types

        return false;
    }
}
