// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;
using RoslynTextChange = Microsoft.CodeAnalysis.Text.TextChange;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public static class RoslynTextChangeExtensions
    {
        public static ITextChange ToVisualStudioTextChange(this RoslynTextChange roslynTextChange) =>
            new VisualStudioTextChange(roslynTextChange.Span.Start, roslynTextChange.Span.Length, roslynTextChange.NewText);
    }
}
