// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal static class RazorSourceDocumentExtensions
    {
        public static int GetAbsoluteIndex(this RazorSourceDocument sourceDocument, Position position)
        {
            if (sourceDocument == null)
            {
                throw new ArgumentNullException(nameof(sourceDocument));
            }

            if (position.Character < 0 || position.Line < 0 || position.Line > sourceDocument.Lines.Count)
            {
                // This should never be possible but we're just hard crashing to know if our expectations are wrong.
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            var lineAbsoluteIndex = sourceDocument.Lines.GetLineStart((int)position.Line);
            var absoluteIndex = lineAbsoluteIndex + (int)position.Character;

            return absoluteIndex;
        }
    }
}
