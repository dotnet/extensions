// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Internal logger for function invocation operations shared between <see cref="FunctionInvokingChatClient"/> and <see cref="FunctionInvokingRealtimeSession"/>.
/// </summary>
internal static partial class FunctionInvocationLogger
{
    [LoggerMessage(LogLevel.Debug, "Invoking {MethodName}.", SkipEnabledCheck = true)]
    internal static partial void LogInvoking(ILogger logger, string methodName);

    [LoggerMessage(LogLevel.Trace, "Invoking {MethodName}({Arguments}).", SkipEnabledCheck = true)]
    internal static partial void LogInvokingSensitive(ILogger logger, string methodName, string arguments);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invocation completed. Duration: {Duration}", SkipEnabledCheck = true)]
    internal static partial void LogInvocationCompleted(ILogger logger, string methodName, TimeSpan duration);

    [LoggerMessage(LogLevel.Trace, "{MethodName} invocation completed. Duration: {Duration}. Result: {Result}", SkipEnabledCheck = true)]
    internal static partial void LogInvocationCompletedSensitive(ILogger logger, string methodName, TimeSpan duration, string result);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invocation canceled.")]
    internal static partial void LogInvocationCanceled(ILogger logger, string methodName);

    [LoggerMessage(LogLevel.Error, "{MethodName} invocation failed.")]
    internal static partial void LogInvocationFailed(ILogger logger, string methodName, Exception error);

    [LoggerMessage(LogLevel.Debug, "Reached maximum iteration count of {MaximumIterationsPerRequest}. Stopping function invocation loop.")]
    internal static partial void LogMaximumIterationsReached(ILogger logger, int maximumIterationsPerRequest);

    [LoggerMessage(LogLevel.Debug, "Function '{FunctionName}' requires approval. Converting to approval request.")]
    internal static partial void LogFunctionRequiresApproval(ILogger logger, string functionName);

    [LoggerMessage(LogLevel.Debug, "Processing approval response for '{FunctionName}'. Approved: {Approved}")]
    internal static partial void LogProcessingApprovalResponse(ILogger logger, string functionName, bool approved);

    [LoggerMessage(LogLevel.Debug, "Function '{FunctionName}' was rejected. Reason: {Reason}")]
    internal static partial void LogFunctionRejected(ILogger logger, string functionName, string? reason);

    [LoggerMessage(LogLevel.Warning, "Maximum consecutive errors ({MaxErrors}) exceeded. Throwing aggregated exceptions.")]
    internal static partial void LogMaxConsecutiveErrorsExceeded(ILogger logger, int maxErrors);

    [LoggerMessage(LogLevel.Warning, "Function '{FunctionName}' not found.")]
    internal static partial void LogFunctionNotFound(ILogger logger, string functionName);

    [LoggerMessage(LogLevel.Debug, "Function '{FunctionName}' is not invocable (declaration only). Terminating loop.")]
    internal static partial void LogNonInvocableFunction(ILogger logger, string functionName);

    [LoggerMessage(LogLevel.Debug, "Function '{FunctionName}' requested termination of the processing loop.")]
    internal static partial void LogFunctionRequestedTermination(ILogger logger, string functionName);
}
