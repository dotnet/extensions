// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#pragma warning disable CS0618
using Microsoft.AspNetCore.Razor.Language;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal abstract class RazorSemanticTokensInfoService
    {
        public abstract SemanticTokens GetSemanticTokens(RazorCodeDocument codeDocument);

        public abstract SemanticTokens GetSemanticTokens(RazorCodeDocument codeDocument, Range range);

        public abstract SemanticTokensFullOrDelta GetSemanticTokensEdits(RazorCodeDocument codeDocument, string previousId);
    }
}
