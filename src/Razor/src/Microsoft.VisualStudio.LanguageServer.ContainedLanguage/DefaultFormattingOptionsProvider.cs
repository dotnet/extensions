// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Composition;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    [Shared]
    [Export(typeof(FormattingOptionsProvider))]
    internal class DefaultFormattingOptionsProvider : FormattingOptionsProvider
    {
        private readonly LSPDocumentManager _documentManager;
        private readonly IIndentationManagerService _indentationManagerService;

        [ImportingConstructor]
        public DefaultFormattingOptionsProvider(
            LSPDocumentManager documentManager,
            IIndentationManagerService indentationManagerService)
        {
            if (documentManager is null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            if (indentationManagerService is null)
            {
                throw new ArgumentNullException(nameof(indentationManagerService));
            }

            _documentManager = documentManager;
            _indentationManagerService = indentationManagerService;
        }

        public override FormattingOptions? GetOptions(Uri lspDocumentUri)
        {
            if (lspDocumentUri is null)
            {
                throw new ArgumentNullException(nameof(lspDocumentUri));
            }

            if (!_documentManager.TryGetDocument(lspDocumentUri, out var documentSnapshot))
            {
                // Couldn't resolve document and therefore can't resolve the corresponding formatting options.
                return null;
            }

            _indentationManagerService.GetIndentation(documentSnapshot.Snapshot.TextBuffer, explicitFormat: false, out var insertSpaces, out var tabSize, out _);
            var formattingOptions = new FormattingOptions()
            {
                InsertSpaces = insertSpaces,
                TabSize = tabSize,
            };

            return formattingOptions;
        }
    }
}
