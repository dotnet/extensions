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
        var index = state.ReservePropertySpace(7);
        var array = state.PropertyArray;
        array[index++] = new("connectionId", connectionId);
        array[index++] = new("type", type);
        array[index++] = new("streamId", streamId);
        array[index++] = new("length", length);
        array[index++] = new("flags", flags);
        array[index++] = new("other", other);
        array[index] = new("{OriginalFormat}", "Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}");

        logger.Log(
            global::Microsoft.Extensions.Logging.LogLevel.Error,
            new global::Microsoft.Extensions.Logging.EventId(0, nameof(RefTypes)),
            state,
            null, // Refer to our docs to learn how to pass exception here
            __FMT_0_RefTypes_Error);

        state.Clear();
    }

    private static string __FMT_0_RefTypes_Error(global::Microsoft.Extensions.Telemetry.Logging.LoggerMessageState _h, global::System.Exception? _)
    {
        var connectionId = _h.PropertyArray[0].Value ?? "(null)";
        var type = _h.PropertyArray[1].Value ?? "(null)";
        var streamId = _h.PropertyArray[2].Value ?? "(null)";
        var length = _h.PropertyArray[3].Value ?? "(null)";
        var flags = _h.PropertyArray[4].Value ?? "(null)";
        var other = _h.PropertyArray[5].Value ?? "(null)";
        return global::System.FormattableString.Invariant($"Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}");
    }

    /// <summary>
    /// Logs "Range [{start}..{end}], options {options}, guid {guid}" at "Error" level.
    /// </summary>
    public static void ValueTypes(global::Microsoft.Extensions.Logging.ILogger logger, long start, long end, int options, global::System.Guid guid)
    {
        var state = global::Microsoft.Extensions.Telemetry.Logging.LoggerMessageHelper.ThreadLocalState;
        var index = state.ReservePropertySpace(5);
        var array = state.PropertyArray;
        array[index++] = new("start", start);
        array[index++] = new("end", end);
        array[index++] = new("options", options);
        array[index++] = new("guid", guid.ToString());
        array[index] = new("{OriginalFormat}", "Range [{start}..{end}], options {options}, guid {guid}");

        logger.Log(
            global::Microsoft.Extensions.Logging.LogLevel.Error,
            new global::Microsoft.Extensions.Logging.EventId(0, nameof(ValueTypes)),
            state,
            null, // Refer to our docs to learn how to pass exception here
            __FMT_2_ValueTypes_Error);

        state.Clear();
    }

    private static string __FMT_2_ValueTypes_Error(global::Microsoft.Extensions.Telemetry.Logging.LoggerMessageState _h, global::System.Exception? _)
    {
        var start = _h.PropertyArray[0].Value;
        var end = _h.PropertyArray[1].Value;
        var options = _h.PropertyArray[2].Value;
        var guid = _h.PropertyArray[3].Value;
        return global::System.FormattableString.Invariant($"Range [{start}..{end}], options {options}, guid {guid}");
    }
}
