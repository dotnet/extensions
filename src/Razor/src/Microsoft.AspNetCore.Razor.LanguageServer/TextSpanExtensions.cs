// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal static class TextSpanExtensions
    {
        public static Range AsRange(this TextSpan span, SourceText sourceText)
        {
            if (sourceText is null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            sourceText.GetLinesAndOffsets(span, out var startLine, out var startChar, out var endLine, out var endChar);

            var range = new Range(
                new Position(startLine, startChar),
                new Position(endLine, endChar));

            return range;
        }
    }
}
