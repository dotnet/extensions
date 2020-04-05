// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class FormattingContext
    {
        public Uri Uri { get; set; }

        public RazorCodeDocument CodeDocument { get; set; }

        public SourceText SourceText => CodeDocument?.GetSourceText();

        public FormattingOptions Options { get; set; }

        public Range Range { get; set; }

        public Dictionary<int, IndentationContext> Indentations { get; } = new Dictionary<int, IndentationContext>();
    }
}
