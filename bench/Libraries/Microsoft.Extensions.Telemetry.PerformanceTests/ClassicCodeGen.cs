// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Bench;

internal static partial class ClassicCodeGen
{
    [LoggerMessage(LogLevel.Error, @"Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}")]
    public static partial void RefTypes(ILogger logger, string connectionId, string type, string streamId, string length, string flags, string other);

    [LoggerMessage(LogLevel.Error, @"Range [{start}..{end}], options {options}, guid {guid}")]
    public static partial void ValueTypes(ILogger logger, long start, long end, int options, Guid guid);
}
