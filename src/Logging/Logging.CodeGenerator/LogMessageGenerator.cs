// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Logging.CodeGenerator
{
    public static class LogMessageGenerator
    {
        private static readonly string EndOfLine = @"
";

        public static IEnumerable<(string fileName, string fileContent)> Generate()
        {
            var infos = Enumerable.Range(0, 7).Select(BuildInfo);

            var fileName = $"LogMessage.cs";
            var fileContent =
$@"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{{
{ForEach(EndOfLine, infos, info => $@"
    /// <summary>
    /// Represents a log message which is pre-computed and strongly typed to reduce logging overhead.
    /// </summary>
{ForEach("", info.details, detail => $@"
    /// <typeparam name=""{detail.type}"">The type of the value in the {detail.position} position of the format string.</typeparam>
")}    public struct LogMessage{info.structTypes}
    {{
        private readonly Action{info.actionTypes} _log;

        /// <summary>
        /// Initializes an instance of the <see cref=""LogMessage{info.doccommentTypes}""/> struct.
        /// </summary>
        /// <param name=""logLevel"">The <see cref=""LogLevel""/> associated with the log.</param>
        /// <param name=""eventId"">The event id associated with the log.</param>
        /// <param name=""formatString"">The named format string</param>
        public LogMessage(LogLevel logLevel, EventId eventId, string formatString)
        {{
            LogLevel = logLevel;
            EventId = eventId;
            FormatString = formatString;
            _log = LoggerMessage.Define{info.structTypes}(logLevel, eventId, formatString);
        }}

        /// <summary>
        /// Initializes an instance of the <see cref=""LogMessage{info.doccommentTypes}""/> struct.
        /// </summary>
        /// <param name=""logLevel"">The <see cref=""LogLevel""/> associated with the log.</param>
        /// <param name=""eventId"">The event id associated with the log.</param>
        /// <param name=""eventName"">The event name associated with the log.</param>
        /// <param name=""formatString"">The named format string</param>
        public LogMessage(LogLevel logLevel, int eventId, string eventName, string formatString) : this(logLevel, new EventId(eventId, eventName), formatString)
        {{
        }}

        /// <summary>
        /// Initializes an instance of the <see cref=""LogMessage{info.doccommentTypes}""/> struct.
        /// </summary>
        /// <param name=""logLevel"">The <see cref=""LogLevel""/> associated with the log.</param>
        /// <param name=""eventId"">The event id associated with the log.</param>
        /// <param name=""formatString"">The named format string</param>
        public LogMessage(LogLevel logLevel, int eventId, string formatString): this(logLevel, new EventId(eventId), formatString)
        {{
        }}

        /// <summary>
        /// Initializes an instance of the <see cref=""LogMessage{info.doccommentTypes}""/> struct.
        /// </summary>
        /// <param name=""logLevel"">The <see cref=""LogLevel""/> associated with the log.</param>
        /// <param name=""eventName"">The event name associated with the log.</param>
        /// <param name=""formatString"">The named format string</param>
        public LogMessage(LogLevel logLevel, string eventName, string formatString): this(logLevel, new EventId(eventName), formatString)
        {{
        }}

        /// <summary>
        /// Gets the <see cref=""LogLevel""/> of this log message.
        /// </summary>
        public LogLevel LogLevel {{ get; }}

        /// <summary>
        /// Gets the <see cref=""EventId""/> of this log message.
        /// </summary>
        public EventId EventId {{ get; }}

        /// <summary>
        /// Gets the format string of this log message.
        /// </summary>
        public string FormatString {{ get; }}

        /// <summary>
        /// Writes a structured log message to registered providers.
        /// </summary>
        /// <param name=""logger"">The <see cref=""ILogger""/> to write to.</param>
{ForEach("", info.details, detail => $@"
        /// <param name=""{detail.arg}"">The value at the {detail.position} position in the format string.</param>
")}        public void Log({Join(", ", "ILogger logger", info.logParameters)})
        {{
            _log({Join(", ", "logger", info.logArguments, "default")});
        }}

        /// <summary>
        /// Writes a structured log message to registered providers with exception details.
        /// </summary>
        /// <param name=""logger"">The <see cref=""ILogger""/> to write to.</param>
        /// <param name=""exception"">The <see cref=""Exception""/> details to include with the log.</param>
{ForEach("", info.details, detail => $@"
        /// <param name=""{detail.arg}"">The value at the {detail.position} position in the format string.</param>
")}       public void Log({Join(", ", "ILogger logger", "Exception exception", info.logParameters)})
        {{
            _log({Join(", ", "logger", info.logArguments, "exception")});
        }}

        /// <summary>
        /// Implicitly initialize the <see cref=""LogMessage{info.doccommentTypes}""/> from the given <see cref=""ValueTuple{{LogLevel, EventId, String}}""/> parameters.
        /// </summary>
        /// <param name=""parameters"">The <see cref=""LogLevel""/>, <see cref=""EventId""/>, and format string to initialize the <see cref=""LogMessage{info.doccommentTypes}""/> struct.</param>
        public static implicit operator LogMessage{info.structTypes}((LogLevel logLevel, EventId eventId, string formatString) parameters)
        {{
            return new LogMessage{info.structTypes}(parameters.logLevel, parameters.eventId, parameters.formatString);
        }}

        /// <summary>
        /// Implicitly initialize the <see cref=""LogMessage{info.doccommentTypes}""/> from the given <see cref=""ValueTuple{{LogLevel, Int32, String, String}}""/> parameters.
        /// </summary>
        /// <param name=""parameters"">The <see cref=""LogLevel""/>, <see cref=""int""/> event id, <see cref=""string""/> event name, and format string to initialize the <see cref=""LogMessage{info.doccommentTypes}""/> struct.</param>
        public static implicit operator LogMessage{info.structTypes}((LogLevel logLevel, int eventId, string eventName, string formatString) parameters)
        {{
            return new LogMessage{info.structTypes}(parameters.logLevel, parameters.eventId, parameters.eventName, parameters.formatString);
        }}

        /// <summary>
        /// Implicitly initialize the <see cref=""LogMessage{info.doccommentTypes}""/> from the given <see cref=""ValueTuple{{LogLevel, Int32, String}}""/> parameters.
        /// </summary>
        /// <param name=""parameters"">The <see cref=""LogLevel""/>, <see cref=""int""/> event id, and format string to initialize the <see cref=""LogMessage{info.doccommentTypes}""/> struct.</param>
        public static implicit operator LogMessage{info.structTypes}((LogLevel logLevel, int eventId, string formatString) parameters)
        {{
            return new LogMessage{info.structTypes}(parameters.logLevel, parameters.eventId, parameters.formatString);
        }}

        /// <summary>
        /// Implicitly initialize the <see cref=""LogMessage{info.doccommentTypes}""/> from the given <see cref=""ValueTuple{{LogLevel, String, String}}""/> parameters.
        /// </summary>
        /// <param name=""parameters"">The <see cref=""LogLevel""/>, <see cref=""string""/> event name, and format string to initialize the <see cref=""LogMessage{info.doccommentTypes}""/> struct.</param>
        public static implicit operator LogMessage{info.structTypes}((LogLevel logLevel, string eventName, string formatString) parameters)
        {{
            return new LogMessage{info.structTypes}(parameters.logLevel, parameters.eventName, parameters.formatString);
        }}
    }}
")}
}}
";
                yield return (fileName, TrimEmptyLine(fileContent));

        }
        private static object Join(string separator, params string[] strings)
        {
            return string.Join(separator, strings.Where(x => !string.IsNullOrEmpty(x)));
        }

        public static (
            int count,
            string structTypes,
            string actionTypes,
            string doccommentTypes,
            string logParameters,
            string logArguments,
            IEnumerable<(string type, string arg, string position)> details)
            BuildInfo(int count)
        {
            var positions = new[] { default, "first", "second", "third", "fourth", "fifth", "sixth" };
            var types = Enumerable.Range(1, count).Select(index => $"T{index}");
            var args = Enumerable.Range(1, count).Select(index => $"value{index}");
            var details = Enumerable.Range(1, count).Select(index => (type: $"T{index}", arg: $"value{index}", position: positions[index]));

            if (count == 0)
            {
                return (0, "", "<ILogger, Exception>", "", "", "", details);
            }

            string structTypes = $"<{string.Join(", ", types)}>";
            string doccommentTypes = $"{{{string.Join(", ", types)}}}";
            string actionTypes = $"<ILogger, {string.Join(", ", types)}, Exception>";
            string logParameters = string.Join(", ", details.Select(detail => $"{detail.type} {detail.arg}"));
            string logArguments = string.Join(", ", details.Select(detail => $"{detail.arg}"));

            return (count, structTypes, actionTypes, doccommentTypes, logParameters, logArguments, details);
        }

        public static string ForEach<T>(string separator, IEnumerable<T> enumerable, Func<T, string> generator)
        {
            return string.Join(separator, enumerable.Select(generator).Select(TrimEmptyLine));
        }

        private static string TrimEmptyLine(string content)
        {
            if (content.StartsWith(EndOfLine))
            {
                content = content.Substring(EndOfLine.Length);
            }
            if (content.EndsWith(EndOfLine + EndOfLine))
            {
                content = content.Substring(0, content.Length - EndOfLine.Length);
            }
            return content;
        }
    }
}
