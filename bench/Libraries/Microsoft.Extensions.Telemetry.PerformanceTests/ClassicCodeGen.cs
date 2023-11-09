// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Bench;

#pragma warning disable S103
#pragma warning disable SA1121
#pragma warning disable S2148
#pragma warning disable IDE0055
#pragma warning disable IDE1006
#pragma warning disable S3235

internal static class ClassicCodeGen
{
    private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.String, global::System.String, global::System.String, global::System.String, global::System.String, global::System.String, global::System.Exception?> __RefTypesCallback =
        global::Microsoft.Extensions.Logging.LoggerMessage.Define<global::System.String, global::System.String, global::System.String, global::System.String, global::System.String, global::System.String>(global::Microsoft.Extensions.Logging.LogLevel.Error, new global::Microsoft.Extensions.Logging.EventId(2037881459, nameof(RefTypes)), "Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

    public static void RefTypes(global::Microsoft.Extensions.Logging.ILogger logger, global::System.String connectionId, global::System.String type, global::System.String streamId, global::System.String length, global::System.String flags, global::System.String other)
    {
        if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Error))
        {
            __RefTypesCallback(logger, connectionId, type, streamId, length, flags, other, null);
        }
    }

    private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.Int64, global::System.Int64, global::System.Int32, global::System.Guid, global::System.Exception?> __ValueTypesCallback =
        global::Microsoft.Extensions.Logging.LoggerMessage.Define<global::System.Int64, global::System.Int64, global::System.Int32, global::System.Guid>(global::Microsoft.Extensions.Logging.LogLevel.Error, new global::Microsoft.Extensions.Logging.EventId(558429541, nameof(ValueTypes)), "Range [{start}..{end}], options {options}, guid {guid}", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

    public static void ValueTypes(global::Microsoft.Extensions.Logging.ILogger logger, global::System.Int64 start, global::System.Int64 end, global::System.Int32 options, global::System.Guid guid)
    {
        if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Error))
        {
            __ValueTypesCallback(logger, start, end, options, guid, null);
        }
    }
}
