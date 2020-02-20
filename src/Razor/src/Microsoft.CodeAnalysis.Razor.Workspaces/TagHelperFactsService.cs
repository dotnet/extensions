// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public abstract class TagHelperFactsService
    {
        public abstract TagHelperBinding GetTagHelperBinding(TagHelperDocumentContext documentContext, string tagName, IEnumerable<KeyValuePair<string, string>> attributes, string parentTag, bool parentIsTagHelper);

        public abstract IEnumerable<BoundAttributeDescriptor> GetBoundTagHelperAttributes(TagHelperDocumentContext documentContext, string attributeName, TagHelperBinding binding);

        public abstract IReadOnlyList<TagHelperDescriptor> GetTagHelpersGivenTag(TagHelperDocumentContext documentContext, string tagName, string parentTag);

        public abstract IReadOnlyList<TagHelperDescriptor> GetTagHelpersGivenParent(TagHelperDocumentContext documentContext, string parentTag);

        // Internal for testing
        internal virtual IEnumerable<KeyValuePair<string, string>> StringifyAttributes(SyntaxList<RazorSyntaxNode> attributes)
        {
            throw new NotImplementedException();
        }

        // Internal for testing
        internal virtual (string ancestorTagName, bool ancestorIsTagHelper) GetNearestAncestorTagInfo(IEnumerable<SyntaxNode> ancestors)
        {
            throw new NotImplementedException();
        }
    }
}
