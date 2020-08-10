// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal abstract class RazorFormattingService
    {
        public abstract Task<TextEdit[]> FormatAsync(
            Uri uri,
            DocumentSnapshot documentSnapshot,
            Range range,
            FormattingOptions options);

        public abstract Task<TextEdit[]> ApplyFormattedEditsAsync(
            Uri uri,
            DocumentSnapshot documentSnapshot,
            RazorLanguageKind kind,
            TextEdit[] formattedEdits,
            FormattingOptions options);
    }
}
