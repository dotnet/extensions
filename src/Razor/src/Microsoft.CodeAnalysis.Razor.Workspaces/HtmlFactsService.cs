// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal abstract class HtmlFactsService
    {
        public abstract bool TryGetElementInfo(SyntaxNode element, out SyntaxToken containingTagNameToken, out SyntaxList<RazorSyntaxNode> attributeNodes);

        public abstract bool TryGetAttributeInfo(
            SyntaxNode attribute,
            out SyntaxToken containingTagNameToken,
            out TextSpan? prefixLocation,
            out string selectedAttributeName,
            out TextSpan? nameLocation,
            out SyntaxList<RazorSyntaxNode> attributeNodes);
    }
}
