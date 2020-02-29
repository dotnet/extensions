// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Shared]
    [Export(typeof(RazorLSPTextDocumentCreatedListener))]
    internal class RazorLSPTextDocumentCreatedListener
    {
        private readonly TrackingLSPDocumentManager _lspDocumentManager;
        private readonly ITextDocumentFactoryService _textDocumentFactory;
        private readonly LSPEditorFeatureDetector _lspEditorFeatureDetector;
        private readonly IContentType _razorLSPContentType;

        [ImportingConstructor]
        public RazorLSPTextDocumentCreatedListener(
            ITextDocumentFactoryService textDocumentFactory,
            IContentTypeRegistryService contentTypeRegistry,
            LSPDocumentManager lspDocumentManager,
            LSPEditorFeatureDetector lspEditorFeatureDetector)
        {
            if (textDocumentFactory is null)
            {
                throw new ArgumentNullException(nameof(textDocumentFactory));
            }

            if (contentTypeRegistry is null)
            {
                throw new ArgumentNullException(nameof(contentTypeRegistry));
            }

            if (lspDocumentManager is null)
            {
                throw new ArgumentNullException(nameof(lspDocumentManager));
            }

            if (lspEditorFeatureDetector is null)
            {
                throw new ArgumentNullException(nameof(lspEditorFeatureDetector));
            }

            _lspDocumentManager = lspDocumentManager as TrackingLSPDocumentManager;

            if (_lspDocumentManager is null)
            {
                throw new ArgumentException("The LSP document manager should be of type " + typeof(TrackingLSPDocumentManager).FullName, nameof(_lspDocumentManager));
            }

            _textDocumentFactory = textDocumentFactory;
            _lspEditorFeatureDetector = lspEditorFeatureDetector;
            _textDocumentFactory.TextDocumentCreated += TextDocumentFactory_TextDocumentCreated;
            _textDocumentFactory.TextDocumentDisposed += TextDocumentFactory_TextDocumentDisposed;
            _razorLSPContentType = contentTypeRegistry.GetContentType(RazorLSPContentTypeDefinition.Name);
        }

        // Internal for testing
        internal void TextDocumentFactory_TextDocumentCreated(object sender, TextDocumentEventArgs args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (!IsRazorLSPTextDocument(args.TextDocument))
            {
                return;
            }

            var textBuffer = args.TextDocument.TextBuffer;
            if (!textBuffer.ContentType.IsOfType(RazorLSPContentTypeDefinition.Name))
            {
                // This Razor text buffer has yet to be initialized.

                InitializeRazorLSPTextBuffer(textBuffer);
            }

            _lspDocumentManager.TrackDocument(textBuffer);
        }

        // Internal for testing
        internal void TextDocumentFactory_TextDocumentDisposed(object sender, TextDocumentEventArgs args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            // Do a lighter check to see if we care about this document.
            if (IsRazorFilePath(args.TextDocument.FilePath))
            {
                // If we don't know about this document we'll no-op
                _lspDocumentManager.UntrackDocument(args.TextDocument.TextBuffer);
            }
        }

        // Internal for testing
        internal bool IsRazorLSPTextDocument(ITextDocument textDocument)
        {
            var filePath = textDocument.FilePath;
            if (filePath == null)
            {
                return false;
            }

            if (!IsRazorFilePath(filePath))
            {
                return false;
            }

            // We pass a `null` hierarchy so we don't eagerly lookup hierarchy information before it's needed.
            if (!_lspEditorFeatureDetector.IsLSPEditorAvailable(textDocument.FilePath, hierarchy: null))
            {
                return false;
            }

            return true;
        }

        private bool IsRazorFilePath(string filePath)
        {
            if (filePath == null)
            {
                return false;
            }

            if (!filePath.EndsWith(RazorLSPContentTypeDefinition.CSHTMLFileExtension, FilePathComparison.Instance) &&
                !filePath.EndsWith(RazorLSPContentTypeDefinition.RazorFileExtension, FilePathComparison.Instance))
            {
                // Not a Razor file
                return false;
            }

            return true;
        }

        private void InitializeRazorLSPTextBuffer(ITextBuffer textBuffer)
        {
            if (_lspEditorFeatureDetector.IsRemoteClient())
            {
                // We purposefully do not set ClientName's in remote client scenarios because we don't want to boot 2 langauge servers (one for both host and client).
                // The ClientName controls whether or not an ILanguageClient instantiates.
            }
            else
            {
                // ClientName controls if the LSP infrastructure in VS will boot when it detects our Razor LSP contennt type. If the property exists then it will; otherwise
                // the text buffer will be ignored by the LSP 
                textBuffer.Properties.AddProperty(LanguageClientConstants.ClientNamePropertyKey, RazorLanguageServerClient.ClientName);
            }

            // This is the default case, basically the buffer is not of the proper content type yet. We need to change it.
            textBuffer.ChangeContentType(_razorLSPContentType, editTag: null);
        }
    }
}
