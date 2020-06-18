// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal static class VSLSPTextSpanExtensions
    {
        public static Range AsLSPRange(this TextSpan span, SourceText sourceText)
        {
            var range = span.AsRange(sourceText);
            return new Range()
            {
                Start = new Position((int)range.Start.Line, (int)range.Start.Character),
                End = new Position((int)range.End.Line, (int)range.End.Character)
            };
        }
    }
}
