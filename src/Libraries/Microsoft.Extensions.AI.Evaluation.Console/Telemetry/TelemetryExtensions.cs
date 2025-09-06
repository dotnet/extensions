// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Utilities;
using Microsoft.Shared.Text;

namespace Microsoft.Extensions.AI.Evaluation.Console.Telemetry;

internal static class TelemetryExtensions
{
    internal static string ToTelemetryPropertyValue(this bool value) =>
        value ? TelemetryConstants.PropertyValues.True : TelemetryConstants.PropertyValues.False;

    internal static string ToTelemetryPropertyValue(this bool? value) =>
        (value ?? false).ToTelemetryPropertyValue();

    internal static string ToTelemetryPropertyValue(this int value) =>
        value.ToInvariantString();

    internal static string ToTelemetryPropertyValue(this int? value) =>
        (value ?? 0).ToInvariantString();

    internal static string ToTelemetryPropertyValue(this long value) =>
        value.ToInvariantString();

    internal static string ToTelemetryPropertyValue(this long? value) =>
        (value ?? 0).ToInvariantString();

    internal static string ToTelemetryPropertyValue(this string? value) =>
        value ?? TelemetryConstants.PropertyValues.Unknown;

    internal static void ReportOperation(
        this TelemetryHelper telemetryHelper,
        string operationName,
        Action operation,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        try
        {
            TimeSpan duration = TimingHelper.ExecuteWithTiming(operation);
            telemetryHelper.ReportOperationSuccess(operationName, duration, properties, metrics);
        }
        catch (Exception ex)
        {
            telemetryHelper.ReportOperationFailure(operationName, ex, properties, metrics);
            throw;
        }
    }

    internal static TResult ReportOperation<TResult>(
        this TelemetryHelper telemetryHelper,
        string operationName,
        Func<TResult> operation,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        try
        {
            (TResult result, TimeSpan duration) = TimingHelper.ExecuteWithTiming(operation);
            telemetryHelper.ReportOperationSuccess(operationName, duration, properties, metrics);
            return result;
        }
        catch (Exception ex)
        {
            telemetryHelper.ReportOperationFailure(operationName, ex, properties, metrics);
            throw;
        }
    }

#pragma warning disable EA0014 // The async method doesn't support cancellation
    internal static async ValueTask ReportOperationAsync(
        this TelemetryHelper telemetryHelper,
        string operationName,
        Func<Task> operation,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        try
        {
            TimeSpan duration = await TimingHelper.ExecuteWithTimingAsync(operation).ConfigureAwait(false);
            telemetryHelper.ReportOperationSuccess(operationName, duration, properties, metrics);
        }
        catch (Exception ex)
        {
            telemetryHelper.ReportOperationFailure(operationName, ex, properties, metrics);
            throw;
        }
    }

    internal static async ValueTask ReportOperationAsync(
        this TelemetryHelper telemetryHelper,
        string operationName,
        Func<ValueTask> operation,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        try
        {
            TimeSpan duration = await TimingHelper.ExecuteWithTimingAsync(operation).ConfigureAwait(false);
            telemetryHelper.ReportOperationSuccess(operationName, duration, properties, metrics);
        }
        catch (Exception ex)
        {
            telemetryHelper.ReportOperationFailure(operationName, ex, properties, metrics);
            throw;
        }
    }

    internal static async ValueTask<TResult> ReportOperationAsync<TResult>(
        this TelemetryHelper telemetryHelper,
        string operationName,
        Func<Task<TResult>> operation,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        try
        {
            (TResult result, TimeSpan duration) =
                await TimingHelper.ExecuteWithTimingAsync(operation).ConfigureAwait(false);

            telemetryHelper.ReportOperationSuccess(operationName, duration, properties, metrics);
            return result;
        }
        catch (Exception ex)
        {
            telemetryHelper.ReportOperationFailure(operationName, ex, properties, metrics);
            throw;
        }
    }

    internal static async ValueTask<TResult> ReportOperationAsync<TResult>(
        this TelemetryHelper telemetryHelper,
        string operationName,
        Func<ValueTask<TResult>> operation,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        try
        {
            (TResult result, TimeSpan duration) =
                await TimingHelper.ExecuteWithTimingAsync(operation).ConfigureAwait(false);

            telemetryHelper.ReportOperationSuccess(operationName, duration, properties, metrics);
            return result;
        }
        catch (Exception ex)
        {
            telemetryHelper.ReportOperationFailure(operationName, ex, properties, metrics);
            throw;
        }
    }

    private static void ReportOperationSuccess(
        this TelemetryHelper telemetryHelper,
        string operationName,
        TimeSpan duration,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        properties ??= new Dictionary<string, string>();
        properties.Add(TelemetryConstants.PropertyNames.Success, TelemetryConstants.PropertyValues.True);

        metrics ??= new Dictionary<string, double>();
        metrics.Add(TelemetryConstants.PropertyNames.DurationInMilliseconds, duration.TotalMilliseconds);

        telemetryHelper.ReportEvent(eventName: operationName, properties, metrics);
    }

    private static void ReportOperationFailure(
        this TelemetryHelper telemetryHelper,
        string operationName,
        Exception exception,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        properties ??= new Dictionary<string, string>();
        properties.Add(TelemetryConstants.PropertyNames.Success, TelemetryConstants.PropertyValues.False);

        metrics ??= new Dictionary<string, double>();
        metrics.Add(TelemetryConstants.PropertyNames.DurationInMilliseconds, 0);

        telemetryHelper.ReportEvent(eventName: operationName, properties, metrics);
        telemetryHelper.ReportException(exception, properties, metrics);
    }
#pragma warning restore EA0014
}
