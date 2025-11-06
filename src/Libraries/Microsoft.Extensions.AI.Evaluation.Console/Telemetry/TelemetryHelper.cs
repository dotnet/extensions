// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.AI.Evaluation.Console.Telemetry;

internal sealed class TelemetryHelper : IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly TelemetryConfiguration? _telemetryConfiguration;
    private readonly TelemetryClient? _telemetryClient;
    private readonly Dictionary<string, string>? _commonProperties;
    private bool _disposed;

    [MemberNotNullWhen(false, nameof(_telemetryConfiguration), nameof(_telemetryClient), nameof(_commonProperties))]
    private bool Disabled { get; }

    internal TelemetryHelper(ILogger logger)
    {
        _logger = logger;

        if (!TelemetryConstants.IsTelemetryEnabled)
        {
            Disabled = true;
            return;
        }

        try
        {
            _telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            _telemetryConfiguration.ConnectionString = TelemetryConstants.ConnectionString;
            _telemetryClient = new TelemetryClient(_telemetryConfiguration);

            var deviceIdHelper = new DeviceIdHelper(logger);
            string deviceId = deviceIdHelper.GetDeviceId();
            string isCIEnvironment = EnvironmentHelper.IsCIEnvironment().ToTelemetryPropertyValue();

            _commonProperties =
                new Dictionary<string, string>
                {
                    [TelemetryConstants.PropertyNames.DevDeviceId] = deviceId,
                    [TelemetryConstants.PropertyNames.OSVersion] = Environment.OSVersion.VersionString,
                    [TelemetryConstants.PropertyNames.OSPlatform] = Environment.OSVersion.Platform.ToString(),
                    [TelemetryConstants.PropertyNames.KernelVersion] = RuntimeInformation.OSDescription,
                    [TelemetryConstants.PropertyNames.RuntimeId] = RuntimeInformation.RuntimeIdentifier,
                    [TelemetryConstants.PropertyNames.ProductVersion] = Constants.Version,
                    [TelemetryConstants.PropertyNames.IsCIEnvironment] = isCIEnvironment
                };

            _telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
            _telemetryClient.Context.Device.OperatingSystem = RuntimeInformation.OSDescription;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to initialize {nameof(TelemetryHelper)}.");

            _telemetryConfiguration?.Dispose();
            _telemetryConfiguration = null;
            _telemetryClient = null;
            _commonProperties = null;

            Disabled = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Disabled || _disposed)
        {
            return;
        }

        try
        {
            _ = await FlushAsync().ConfigureAwait(false);
            _telemetryConfiguration.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to dispose {nameof(TelemetryHelper)}.");
        }

        _disposed = true;
    }

    internal void ReportEvent(
        string eventName,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        if (Disabled || _disposed)
        {
            return;
        }

        try
        {
            IDictionary<string, string>? combinedProperties = GetCombinedProperties(properties);

            _telemetryClient.TrackEvent(
                $"{TelemetryConstants.EventNamespace}/{eventName}",
                combinedProperties,
                metrics);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to report event '{eventName}' in telemetry.");
        }
    }

    internal void ReportException(
        Exception exception,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        if (Disabled || _disposed)
        {
            return;
        }

        try
        {
            IDictionary<string, string>? combinedProperties = GetCombinedProperties(properties);

            _telemetryClient.TrackException(exception, combinedProperties, metrics);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to report exception in telemetry.");
        }
    }

    internal async Task<bool> FlushAsync(CancellationToken cancellationToken = default)
    {
        if (Disabled || _disposed)
        {
            return false;
        }

        try
        {
            return await _telemetryClient.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to flush telemetry.");

            return false;
        }
    }

    private Dictionary<string, string>? GetCombinedProperties(IDictionary<string, string>? properties)
    {
        if (Disabled || _disposed)
        {
            return null;
        }

        Dictionary<string, string> combinedProperties;
        if (properties is null)
        {
            combinedProperties = _commonProperties;
        }
        else
        {
            combinedProperties = new Dictionary<string, string>(_commonProperties);
            foreach (var kvp in properties)
            {
                combinedProperties.Add(kvp.Key, kvp.Value);
            }
        }

        return combinedProperties;
    }
}
