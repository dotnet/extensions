// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [FileExtension(RazorLSPConstants.CSHTMLFileExtension)]
    [Name(nameof(CSHTMLFilePathToContentTypeProvider))]
    [Export(typeof(IFilePathToContentTypeProvider))]
    internal class CSHTMLFilePathToContentTypeProvider : RazorFilePathToContentTypeProviderBase
    {
        [ImportingConstructor]
        public CSHTMLFilePathToContentTypeProvider(
            IContentTypeRegistryService contentTypeRegistryService,
            LSPEditorFeatureDetector lspEditorFeatureDetector,

            // This RazorLSPTextDocumentCreatedListener is imported here for 1 purpose:
            // To ensure that our listener tech gets instantiated early enough in the document creation pipeline to hook into
            // the ITextDocumentFactoryService.TextDocumentCreated/TextDocumentDisposed methods to track Razor document lifetimes.
            RazorLSPTextDocumentCreatedListener listener) : base(contentTypeRegistryService, lspEditorFeatureDetector)
        {
        }
    }
}
