// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

#pragma warning disable S109

namespace Microsoft.Gen.Logging.Bench;

internal static partial class Log
{
    [LogMethod(LogLevel.Error, @"Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}")]
    public static partial void RefTypes_Error(ILogger logger, string connectionId, string type, string streamId, string length, string flags, string other);

    [LogMethod(LogLevel.Debug, @"Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}")]
    public static partial void RefTypes_Debug(ILogger logger, string connectionId, string type, string streamId, string length, string flags, string other);

    [LogMethod(LogLevel.Error, @"Range [{start}..{end}], options {options}, guid {guid}")]
    public static partial void ValueTypes_Error(ILogger logger, long start, long end, int options, Guid guid);

    [LogMethod(LogLevel.Debug, @"Range [{start}..{end}], options {options}, guid {guid}")]
    public static partial void ValueTypes_Debug(ILogger logger, long start, long end, int options, Guid guid);
}
