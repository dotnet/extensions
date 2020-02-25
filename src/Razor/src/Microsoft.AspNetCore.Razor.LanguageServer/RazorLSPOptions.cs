// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorLSPOptions : IEquatable<RazorLSPOptions>
    {
        public RazorLSPOptions(Trace trace, bool enableFormatting)
        {
            Trace = trace;
            EnableFormatting = enableFormatting;
        }

        public static RazorLSPOptions Default => new RazorLSPOptions(trace: default, enableFormatting: true);

        public Trace Trace { get; }

        public LogLevel MinLogLevel => GetLogLevelForTrace(Trace);

        public bool EnableFormatting { get; }

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
                EnableFormatting == other.EnableFormatting;
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
            return hash;
        }
    }
}
