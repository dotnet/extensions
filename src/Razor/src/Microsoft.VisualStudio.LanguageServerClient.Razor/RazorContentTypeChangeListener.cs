// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Name(nameof(RazorContentTypeChangeListener))]
    [Export(typeof(ITextBufferContentTypeListener))]
    [ContentType(RazorLSPConstants.RazorLSPContentTypeName)]
    internal class RazorContentTypeChangeListener : ITextBufferContentTypeListener
    {
        private readonly TrackingLSPDocumentManager _lspDocumentManager;
        private readonly ITextDocumentFactoryService _textDocumentFactory;
        private readonly LSPEditorFeatureDetector _lspEditorFeatureDetector;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IEditorOptionsFactoryService _editorOptionsFactory;

        [ImportingConstructor]
        public RazorContentTypeChangeListener(
            ITextDocumentFactoryService textDocumentFactory,
            LSPDocumentManager lspDocumentManager,
            LSPEditorFeatureDetector lspEditorFeatureDetector,
            SVsServiceProvider serviceProvider,
            IEditorOptionsFactoryService editorOptionsFactory)
        {
            if (textDocumentFactory is null)
            {
                throw new ArgumentNullException(nameof(textDocumentFactory));
            }

            if (lspDocumentManager is null)
            {
                throw new ArgumentNullException(nameof(lspDocumentManager));
            }

            if (lspEditorFeatureDetector is null)
            {
                throw new ArgumentNullException(nameof(lspEditorFeatureDetector));
            }

            if (serviceProvider is null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (editorOptionsFactory is null)
            {
                throw new ArgumentNullException(nameof(editorOptionsFactory));
            }

            _lspDocumentManager = lspDocumentManager as TrackingLSPDocumentManager;

            if (_lspDocumentManager is null)
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentException("The LSP document manager should be of type " + typeof(TrackingLSPDocumentManager).FullName, nameof(_lspDocumentManager));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }

            _textDocumentFactory = textDocumentFactory;
            _lspEditorFeatureDetector = lspEditorFeatureDetector;
            _serviceProvider = serviceProvider;
            _editorOptionsFactory = editorOptionsFactory;
        }

        public void ContentTypeChanged(ITextBuffer textBuffer, IContentType oldContentType, IContentType newContentType)
        {
            var supportedBefore = oldContentType.IsOfType(RazorLSPConstants.RazorLSPContentTypeName);
            var supportedAfter = newContentType.IsOfType(RazorLSPConstants.RazorLSPContentTypeName);

            if (supportedBefore == supportedAfter)
            {
                // We went from a Razor content type to another Razor content type.
                return;
            }

            if (supportedAfter)
            {
                RazorBufferCreated(textBuffer);
            }
            else if (supportedBefore)
            {
                RazorBufferDisposed(textBuffer);
            }
        }

        // Internal for testing
        internal void RazorBufferCreated(ITextBuffer textBuffer)
        {
            if (textBuffer is null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            if (!_lspEditorFeatureDetector.IsRemoteClient())
            {
                // Renames on open files don't dispose buffer state so we need to separately monitor the buffer for document renames to ensure
                // we can tell the lsp document manager when state changes.
                MonitorDocumentForRenames(textBuffer);

                // Only need to track documents on a host because we don't do any extra work on remote clients.
                _lspDocumentManager.TrackDocument(textBuffer);
            }
        }

        // Internal for testing
        internal void RazorBufferDisposed(ITextBuffer textBuffer)
        {
            if (textBuffer is null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            StopMonitoringDocumentForRenames(textBuffer);

            // If we don't know about this document we'll no-op
            _lspDocumentManager.UntrackDocument(textBuffer);
        }

        // Internal for testing
        internal void TextDocument_FileActionOccurred(object sender, TextDocumentFileActionEventArgs args)
        {
            if (args.FileActionType != FileActionTypes.DocumentRenamed)
            {
                // We're only interested in document rename events.
                return;
            }

            var textDocument = sender as ITextDocument;
            var textBuffer = textDocument?.TextBuffer;

            if (textBuffer == null)
            {
                return;
            }

            // Document was renamed, translate that rename into an untrack -> track to refresh state.

            _lspDocumentManager.UntrackDocument(textBuffer);
            _lspDocumentManager.TrackDocument(textBuffer);
        }

        private void MonitorDocumentForRenames(ITextBuffer textBuffer)
        {
            if (!_textDocumentFactory.TryGetTextDocument(textBuffer, out var textDocument))
            {
                // Cannot monitor buffers that don't have an associated text document. In practice, this should never happen but being extra defensive here.
                return;
            }

            textDocument.FileActionOccurred += TextDocument_FileActionOccurred;
        }

        private void StopMonitoringDocumentForRenames(ITextBuffer textBuffer)
        {
            if (!_textDocumentFactory.TryGetTextDocument(textBuffer, out var textDocument))
            {
                // Text document must have been torn down, no need to unsubscribe to something that's already been torn down.
                return;
            }

            textDocument.FileActionOccurred -= TextDocument_FileActionOccurred;
        }
    }
}
