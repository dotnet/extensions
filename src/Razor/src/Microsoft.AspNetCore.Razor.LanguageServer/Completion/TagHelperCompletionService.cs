// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal abstract class TagHelperCompletionService
    {
        public abstract IReadOnlyList<CompletionItem> GetCompletionsAt(SourceSpan location, RazorCodeDocument codeDocument);
    }
}
