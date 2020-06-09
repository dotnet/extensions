// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal abstract class RazorSemanticTokenInfoService
    {
        public abstract SemanticTokens GetSemanticTokens(RazorCodeDocument codeDocument, SourceLocation? location = null);
    }
}
