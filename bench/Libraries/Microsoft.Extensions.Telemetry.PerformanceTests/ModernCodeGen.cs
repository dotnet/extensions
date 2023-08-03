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

        _ = state.ReservePropertySpace(7);
        state.PropertyArray[6] = new("connectionId", connectionId);
        state.PropertyArray[5] = new("type", type);
        state.PropertyArray[4] = new("streamId", streamId);
        state.PropertyArray[3] = new("length", length);
        state.PropertyArray[2] = new("flags", flags);
        state.PropertyArray[1] = new("other", other);
        state.PropertyArray[0] = new("{OriginalFormat}", "Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}");

        logger.Log(
            global::Microsoft.Extensions.Logging.LogLevel.Error,
            new global::Microsoft.Extensions.Logging.EventId(0, nameof(RefTypes)),
            state,
            null,
            static (s, _) =>
            {
                var connectionId = s.PropertyArray[6].Value ?? "(null)";
                var type = s.PropertyArray[5].Value ?? "(null)";
                var streamId = s.PropertyArray[4].Value ?? "(null)";
                var length = s.PropertyArray[3].Value ?? "(null)";
                var flags = s.PropertyArray[2].Value ?? "(null)";
                var other = s.PropertyArray[1].Value ?? "(null)";
                return global::System.FormattableString.Invariant($"Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}");
            });

        state.Clear();
    }

    /// <summary>
    /// Logs "Range [{start}..{end}], options {options}, guid {guid}" at "Error" level.
    /// </summary>
    public static void ValueTypes(global::Microsoft.Extensions.Logging.ILogger logger, long start, long end, int options, global::System.Guid guid)
    {
        var state = global::Microsoft.Extensions.Telemetry.Logging.LoggerMessageHelper.ThreadLocalState;

        _ = state.ReservePropertySpace(5);
        state.PropertyArray[4] = new("start", start);
        state.PropertyArray[3] = new("end", end);
        state.PropertyArray[2] = new("options", options);
        state.PropertyArray[1] = new("guid", guid.ToString());
        state.PropertyArray[0] = new("{OriginalFormat}", "Range [{start}..{end}], options {options}, guid {guid}");

        logger.Log(
            global::Microsoft.Extensions.Logging.LogLevel.Error,
            new global::Microsoft.Extensions.Logging.EventId(0, nameof(ValueTypes)),
            state,
            null,
            static (s, _) =>
            {
                var start = s.PropertyArray[4].Value;
                var end = s.PropertyArray[3].Value;
                var options = s.PropertyArray[2].Value;
                var guid = s.PropertyArray[1].Value;
                return global::System.FormattableString.Invariant($"Range [{start}..{end}], options {options}, guid {guid}");
            });

        state.Clear();
    }
}
