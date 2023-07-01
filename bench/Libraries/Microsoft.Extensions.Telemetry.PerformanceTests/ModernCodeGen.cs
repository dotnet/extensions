// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Telemetry.Bench;

[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Emulating generated code")]
internal static class ModernCodeGen
{
    /// <summary>
    /// Logs "Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}" at "Error" level.
    /// </summary>
    public static void RefTypes(global::Microsoft.Extensions.Logging.ILogger logger, string connectionId, string type, string streamId, string length, string flags, string other)
    {
        var state = global::Microsoft.Extensions.Telemetry.Logging.LoggerMessageHelper.ThreadLocalState;
        var sp = state.AllocPropertySpace(7);
        sp[0] = new("connectionId", connectionId);
        sp[1] = new("type", type);
        sp[2] = new("streamId", streamId);
        sp[3] = new("length", length);
        sp[4] = new("flags", flags);
        sp[5] = new("other", other);
        sp[6] = new("{OriginalFormat}", "Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}");

        logger.Log(
            global::Microsoft.Extensions.Logging.LogLevel.Error,
            new global::Microsoft.Extensions.Logging.EventId(0, nameof(RefTypes)),
            state,
            null, // Refer to our docs to learn how to pass exception here
            __FMT_0_RefTypes_Error);
    }

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

    /// <summary>
    /// Logs "Range [{start}..{end}], options {options}, guid {guid}" at "Error" level.
    /// </summary>
    public static void ValueTypes(global::Microsoft.Extensions.Logging.ILogger logger, long start, long end, int options, global::System.Guid guid)
    {
        var state = global::Microsoft.Extensions.Telemetry.Logging.LoggerMessageHelper.ThreadLocalState;
        var sp = state.AllocPropertySpace(5);
        sp[0] = new("start", start);
        sp[1] = new("end", end);
        sp[2] = new("options", options);
        sp[3] = new("guid", guid.ToString());
        sp[4] = new("{OriginalFormat}", "Range [{start}..{end}], options {options}, guid {guid}");

        logger.Log(
            global::Microsoft.Extensions.Logging.LogLevel.Error,
            new global::Microsoft.Extensions.Logging.EventId(0, nameof(ValueTypes)),
            state,
            null, // Refer to our docs to learn how to pass exception here
            __FMT_2_ValueTypes_Error);
    }

    private static string __FMT_2_ValueTypes_Error(global::Microsoft.Extensions.Telemetry.Logging.LoggerMessageState _h, global::System.Exception? _)
    {
        var start = _h.Properties[0].Value;
        var end = _h.Properties[1].Value;
        var options = _h.Properties[2].Value;
        var guid = _h.Properties[3].Value;
        return global::System.FormattableString.Invariant($"Range [{start}..{end}], options {options}, guid {guid}");
    }
}
