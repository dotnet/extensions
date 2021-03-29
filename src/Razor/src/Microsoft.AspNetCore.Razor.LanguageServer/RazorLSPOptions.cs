// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorLSPOptions : IEquatable<RazorLSPOptions>
    {
        public RazorLSPOptions(Trace trace, bool enableFormatting, bool autoClosingTags, bool insertSpaces, int tabSize)
        {
            Trace = trace;
            EnableFormatting = enableFormatting;
            AutoClosingTags = autoClosingTags;
            TabSize = tabSize;
            InsertSpaces = insertSpaces;
        }

        public static RazorLSPOptions Default =>
            new RazorLSPOptions(trace: default, enableFormatting: true, autoClosingTags: true, insertSpaces: true, tabSize: 4);

        public Trace Trace { get; }

        public LogLevel MinLogLevel => GetLogLevelForTrace(Trace);

        public bool EnableFormatting { get; }

        public bool AutoClosingTags { get; }

        public int TabSize { get; }

        public bool InsertSpaces { get; }

        public static LogLevel GetLogLevelForTrace(Trace trace)
        {
            return trace switch
            {
                Trace.Off => LogLevel.None,
                Trace.Messages => LogLevel.Information,
                Trace.Verbose => LogLevel.Trace,
                _ => LogLevel.None,
            };
        }

        public bool Equals(RazorLSPOptions other)
        {
            return
                other != null &&
                Trace == other.Trace &&
                EnableFormatting == other.EnableFormatting &&
                AutoClosingTags == other.AutoClosingTags &&
                InsertSpaces == other.InsertSpaces &&
                TabSize == other.TabSize;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RazorLSPOptions);
        }

        public override int GetHashCode()
        {
            var hash = new HashCodeCombiner();
            hash.Add(Trace);
            hash.Add(EnableFormatting);
            hash.Add(AutoClosingTags);
            hash.Add(InsertSpaces);
            hash.Add(TabSize);
            return hash;
        }
    }
}
