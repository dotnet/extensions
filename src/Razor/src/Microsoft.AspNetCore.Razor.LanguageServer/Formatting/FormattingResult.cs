// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal struct FormattingResult
    {
        public FormattingResult(TextEdit[] edits, RazorLanguageKind kind = RazorLanguageKind.Razor)
        {
            if (edits is null)
            {
                throw new ArgumentNullException(nameof(edits));
            }

            Edits = edits;
            Kind = kind;
        }

        public TextEdit[] Edits { get; }

        public RazorLanguageKind Kind { get; }
    }
}
