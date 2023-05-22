// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Resilience.Internal;

[SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Generators.")]
[SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "Generators.")]
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Readability.")]
internal static partial class Log
{
    [LogMethod(0, LogLevel.Warning,
        "Fallback policy: {policyName}. " +
        "Request failed with the reason: {reason}. " +
        "Performing fallback.")]
    public static partial void LogFallback(
        ILogger logger,
        string policyName,
        string reason);

    [LogMethod(1, LogLevel.Error,
        "Circuit breaker policy: {policyName}. " +
        "Circuit has been broken for {seconds} seconds. " +
        "The reason: {reason}.")]
    public static partial void LogCircuitBreak(
        ILogger logger,
        string policyName,
        double seconds,
        string reason);

    [LogMethod(2, LogLevel.Information,
        "Circuit breaker policy: {policyName}. " +
        "Reset has been triggered.")]
    public static partial void LogCircuitReset(
        ILogger logger,
        string policyName);

    [LogMethod(3, LogLevel.Warning,
        "Retry policy: {policyName}. " +
        "Request failed with the reason: {reason}. " +
        "Waiting {seconds} seconds before next retry. " +
        "Retry attempt {attemptNumber}.")]
    public static partial void LogRetry(
        ILogger logger,
        string policyName,
        string reason,
        double seconds,
        int attemptNumber);

    [LogMethod(4, LogLevel.Warning,
        "Bulkhead policy: {policyName}. " +
        "Bulkhead policy has been triggered.")]
    public static partial void LogBulkhead(
        ILogger logger,
        string policyName);

    [LogMethod(5,
        LogLevel.Warning,
        "Hedging policy: {policyName}. " +
        "Request failed with the reason: {reason}. " +
        "Performing hedging.")]
    public static partial void LogHedging(
        ILogger logger,
        string policyName,
        string reason);

    [LogMethod(6,
        LogLevel.Warning,
        "Timeout policy: {policyName}. " +
        "Timeout interval has been reached.")]
    public static partial void LogTimeout(
        ILogger logger,
        string policyName);

    [LogMethod(7, LogLevel.Information, "Circuit breaker policy: {policyName}. Half-Open has been triggered.")]
    public static partial void LogCircuitHalfOpen(ILogger logger, string policyName);
}
