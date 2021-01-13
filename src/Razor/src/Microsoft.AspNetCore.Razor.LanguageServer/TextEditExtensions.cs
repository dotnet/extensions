// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal static class TextEditExtensions
    {
        public static TextChange AsTextChange(this TextEdit textEdit, SourceText sourceText)
        {
            if (textEdit is null)
            {
                throw new ArgumentNullException(nameof(textEdit));
            }

            if (sourceText is null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            var span = textEdit.Range.AsTextSpan(sourceText);
            return new TextChange(span, textEdit.NewText);
        }
    }
}
