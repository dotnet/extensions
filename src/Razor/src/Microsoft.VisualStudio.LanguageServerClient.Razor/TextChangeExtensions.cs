// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public static class TextChangeExtensions
    {
        public static bool IsDelete(this TextChange textChange)
        {
            return textChange.Span.Length > 0 && textChange.NewText.Length == 0;
        }

        public static bool IsInsert(this TextChange textChange)
        {
            return textChange.Span.Length == 0 && textChange.NewText.Length > 0;
        }

        public static bool IsReplace(this TextChange textChange)
        {
            return textChange.Span.Length > 0 && textChange.NewText.Length > 0;
        }
    }
}
