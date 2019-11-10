// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Logging.CodeGenerator
{
    public static class LogScopeGenerator
    {
        private static readonly string EndOfLine = @"
";


        public static IEnumerable<(string fileName, string fileContent)> Generate()
        {
            var infos = Enumerable.Range(0, 3).Select(BuildInfo);

            var fileName = $"LogScope.cs";
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
")}    public struct LogScope{info.structTypes}
    {{
        private readonly Func{info.delegateTypes} _scope;

        /// <summary>
        /// Initializes an instance of the <see cref=""LogScope{info.doccommentTypes}""/> struct.
        /// </summary>
        /// <param name=""formatString"">The scope format string</param>
        public LogScope(string formatString)
        {{
            FormatString = formatString;
            _scope = LoggerMessage.DefineScope{info.structTypes}(formatString);
        }}

        /// <summary>
        /// Gets the format string of this log scope.
        /// </summary>
        public string FormatString {{ get; }}

        /// <summary>
        /// Begins a structured log scope on registered providers.
        /// </summary>
        /// <param name=""logger"">The <see cref=""ILogger""/> to write to.</param>
{ForEach("", info.details, detail => $@"
        /// <param name=""{detail.arg}"">The value at the {detail.position} position in the format string.</param>
")}        /// <returns>A disposable scope object. Can be null.</returns>
        public IDisposable Begin({Join(", ", "ILogger logger", info.scopeParameters)})
        {{
            return _scope({Join(", ", "logger", info.scopeArguments)});
        }}

        /// <summary>
        /// Implicitly initialize the <see cref=""LogScope{info.doccommentTypes}""/> from the given parameters.
        /// </summary>
        /// <param name=""formatString"">The format string to initialize the <see cref=""LogScope{info.doccommentTypes}""/> struct.</param>
        public static implicit operator LogScope{info.structTypes}(string formatString)
        {{
            return new LogScope{info.structTypes}(formatString);
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
            string delegateTypes,
            string doccommentTypes,
            string scopeParameters,
            string scopeArguments,
            IEnumerable<(string type, string arg, string position)> details)
            BuildInfo(int count)
        {
            var positions = new[] { default, "first", "second", "third", "fourth", "fifth", "sixth" };
            var types = Enumerable.Range(1, count).Select(index => $"T{index}");
            var args = Enumerable.Range(1, count).Select(index => $"value{index}");
            var details = Enumerable.Range(1, count).Select(index => (type: $"T{index}", arg: $"value{index}", position: positions[index]));

            if (count == 0)
            {
                return (0, "", "<ILogger, IDisposable>", "", "", "", details);
            }

            string structTypes = $"<{string.Join(", ", types)}>";
            string doccommentTypes = $"{{{string.Join(", ", types)}}}";
            string delegateTypes = $"<ILogger, {string.Join(", ", types)}, IDisposable>";
            string scopeParameters = string.Join(", ", details.Select(detail => $"{detail.type} {detail.arg}"));
            string scopeArguments = string.Join(", ", details.Select(detail => $"{detail.arg}"));

            return (count, structTypes, delegateTypes, doccommentTypes, scopeParameters, scopeArguments, details);
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
