// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Telemetry.Bench;

internal static partial class ModernCodeGen
{
    /// <summary>
    /// Logs "Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}" at "Error" level.
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Gen.Logging", "8.0.0.0")]
    public static void RefTypes(global::Microsoft.Extensions.Logging.ILogger logger, string connectionId, string type, string streamId, string length, string flags, string other)
    {
        var _helper = global::Microsoft.Extensions.Telemetry.Logging.LoggerMessageHelper.ThreadLocalState;
        _helper.AddProperty("connectionId", connectionId);
        _helper.AddProperty("type", type);
        _helper.AddProperty("streamId", streamId);
        _helper.AddProperty("length", length);
        _helper.AddProperty("flags", flags);
        _helper.AddProperty("other", other);
        _helper.AddProperty("{OriginalFormat}", "Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}");

        logger.Log(
            global::Microsoft.Extensions.Logging.LogLevel.Error,
            new EventId(0, nameof(RefTypes)),
            _helper,
            null, // Refer to our docs to learn how to pass exception here
            __FUNC_0_RefTypes_Error);
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Gen.Logging", "8.0.0.0")]
    private static string __FMT_0_RefTypes_Error(global::Microsoft.Extensions.Telemetry.Logging.LoggerMessageState _h, global::System.Exception? _)
    {
        var connectionId = _h.Properties[0].Value ?? "(null)";
        var type = _h.Properties[1].Value ?? "(null)";
        var streamId = _h.Properties[2].Value ?? "(null)";
        var length = _h.Properties[3].Value ?? "(null)";
        var flags = _h.Properties[4].Value ?? "(null)";
        var other = _h.Properties[5].Value ?? "(null)";
        return global::System.FormattableString.Invariant($"Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}");
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Gen.Logging", "8.0.0.0")]
    private static readonly global::System.Func<global::Microsoft.Extensions.Telemetry.Logging.LoggerMessageState, global::System.Exception?, string> __FUNC_0_RefTypes_Error = new(__FMT_0_RefTypes_Error);

    /// <summary>
    /// Logs "Range [{start}..{end}], options {options}, guid {guid}" at "Error" level.
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Gen.Logging", "8.0.0.0")]
    public static void ValueTypes(global::Microsoft.Extensions.Logging.ILogger logger, long start, long end, int options, global::System.Guid guid)
    {
        var _helper = global::Microsoft.Extensions.Telemetry.Logging.LoggerMessageHelper.ThreadLocalState;
        _helper.AddProperty("start", start);
        _helper.AddProperty("end", end);
        _helper.AddProperty("options", options);
        _helper.AddProperty("guid", guid.ToString());
        _helper.AddProperty("{OriginalFormat}", "Range [{start}..{end}], options {options}, guid {guid}");

        logger.Log(
            global::Microsoft.Extensions.Logging.LogLevel.Error,
            new EventId(0, nameof(ValueTypes)),
            _helper,
            null, // Refer to our docs to learn how to pass exception here
            __FUNC_2_ValueTypes_Error);
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Gen.Logging", "8.0.0.0")]
    private static string __FMT_2_ValueTypes_Error(global::Microsoft.Extensions.Telemetry.Logging.LoggerMessageState _h, global::System.Exception? _)
    {
        var start = _h.Properties[0].Value;
        var end = _h.Properties[1].Value;
        var options = _h.Properties[2].Value;
        var guid = _h.Properties[3].Value;
        return global::System.FormattableString.Invariant($"Range [{start}..{end}], options {options}, guid {guid}");
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Gen.Logging", "8.0.0.0")]
    private static readonly global::System.Func<global::Microsoft.Extensions.Telemetry.Logging.LoggerMessageState, global::System.Exception?, string> __FUNC_2_ValueTypes_Error = new(__FMT_2_ValueTypes_Error);
}
