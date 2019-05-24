// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Completion
{
    [Shared]
    [Export(typeof(RazorCompletionFactsService))]
    internal class DefaultRazorCompletionFactsService : RazorCompletionFactsService
    {
        private readonly IReadOnlyList<RazorCompletionItemProvider> _completionItemProviders;

        [ImportingConstructor]
        public DefaultRazorCompletionFactsService([ImportMany] IEnumerable<RazorCompletionItemProvider> completionItemProviders)
        {
            if (completionItemProviders is null)
            {
                throw new ArgumentNullException(nameof(completionItemProviders));
            }

            _completionItemProviders = completionItemProviders.ToArray();
        }

        public override IReadOnlyList<RazorCompletionItem> GetCompletionItems(RazorSyntaxTree syntaxTree, TagHelperDocumentContext tagHelperDocumentContext, SourceSpan location)
        {
            if (syntaxTree is null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            if (tagHelperDocumentContext is null)
            {
                throw new ArgumentNullException(nameof(tagHelperDocumentContext));
            }

            var completions = new List<RazorCompletionItem>();
            for (var i = 0; i < _completionItemProviders.Count; i++)
            {
                var completionItemProvider = _completionItemProviders[i];
                var items = completionItemProvider.GetCompletionItems(syntaxTree, tagHelperDocumentContext, location);
                completions.AddRange(items);
            }

            return completions;
        }
    }
}
