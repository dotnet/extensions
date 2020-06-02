// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Linq;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Shared]
    [Export(typeof(RazorLSPTextDocumentCreatedListener))]
    internal class RazorLSPTextDocumentCreatedListener
    {
        private static readonly Guid HtmlLanguageServiceGuid = new Guid("9BBFD173-9770-47DC-B191-651B7FF493CD");

        private const string FilePathPropertyKey = "RazorTextBufferFilePath";

        private readonly TrackingLSPDocumentManager _lspDocumentManager;
        private readonly ITextDocumentFactoryService _textDocumentFactory;
        private readonly LSPEditorFeatureDetector _lspEditorFeatureDetector;
        private readonly LSPEditorService _editorService;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IEditorOptionsFactoryService _editorOptionsFactory;
        private readonly IContentType _razorLSPContentType;

        [ImportingConstructor]
        public RazorLSPTextDocumentCreatedListener(
            ITextDocumentFactoryService textDocumentFactory,
            IContentTypeRegistryService contentTypeRegistry,
            LSPDocumentManager lspDocumentManager,
            LSPEditorFeatureDetector lspEditorFeatureDetector,
            LSPEditorService editorService,
            SVsServiceProvider serviceProvider,
            IEditorOptionsFactoryService editorOptionsFactory)
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

            if (editorService is null)
            {
                throw new ArgumentNullException(nameof(editorService));
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
            _editorService = editorService;
            _serviceProvider = serviceProvider;
            _editorOptionsFactory = editorOptionsFactory;

            _textDocumentFactory.TextDocumentCreated += TextDocumentFactory_TextDocumentCreated;
            _textDocumentFactory.TextDocumentDisposed += TextDocumentFactory_TextDocumentDisposed;
            _razorLSPContentType = contentTypeRegistry.GetContentType(RazorLSPConstants.RazorLSPContentTypeName);
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
            if (!textBuffer.ContentType.IsOfType(RazorLSPConstants.RazorLSPContentTypeName))
            {
                // This Razor text buffer has yet to be initialized.

                InitializeRazorLSPTextBuffer(args.TextDocument.FilePath, textBuffer);
            }
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

                if (_lspEditorFeatureDetector.IsRemoteClient() &&
                    args.TextDocument.TextBuffer.Properties.ContainsProperty(FilePathPropertyKey))
                {
                    // We no longer want to watch for guest buffer changes.
                    args.TextDocument.TextBuffer.Properties.RemoveProperty(FilePathPropertyKey);
                    args.TextDocument.TextBuffer.ChangedHighPriority -= RazorGuestBuffer_Changed;
                }
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

        private static bool IsRazorFilePath(string filePath)
        {
            if (filePath == null)
            {
                return false;
            }

            if (!filePath.EndsWith(RazorLSPConstants.CSHTMLFileExtension, FilePathComparison.Instance) &&
                !filePath.EndsWith(RazorLSPConstants.RazorFileExtension, FilePathComparison.Instance))
            {
                // Not a Razor file
                return false;
            }

            return true;
        }

        private void InitializeRazorLSPTextBuffer(string filePath, ITextBuffer textBuffer)
        {
            if (_lspEditorFeatureDetector.IsRemoteClient())
            {
                // We purposefully do not set ClientName's in remote client scenarios because we don't want to boot 2 langauge servers (one for both host and client).
                // The ClientName controls whether or not an ILanguageClient instantiates.

                // We still change the content type for remote scenarios in order to enable our TextMate grammar to light up the Razor editor properly.
                textBuffer.ChangeContentType(_razorLSPContentType, editTag: null);

                // Initialize the buffer with editor options.
                // Temporary: Ideally in remote scenarios, we should be using host's settings.
                // But we need this until that support is built.
                InitializeOptions(textBuffer);

                // Temporary: The guest needs to react to the host manually applying edits and moving the cursor.
                // This can be removed once the client starts supporting snippets.
                textBuffer.Properties.AddProperty(FilePathPropertyKey, filePath);
                textBuffer.ChangedHighPriority += RazorGuestBuffer_Changed;
            }
            else
            {
                // ClientName controls if the LSP infrastructure in VS will boot when it detects our Razor LSP contennt type. If the property exists then it will; otherwise
                // the text buffer will be ignored by the LSP 
                textBuffer.Properties.AddProperty(LanguageClientConstants.ClientNamePropertyKey, RazorLanguageServerClient.ClientName);

                // Initialize the buffer with editor options.
                InitializeOptions(textBuffer);

                textBuffer.ChangeContentType(_razorLSPContentType, editTag: null);

                // Must track the document after changing the content type so any LSPDocuments created understand they're being created for a Razor LSP document.
                _lspDocumentManager.TrackDocument(textBuffer);
            }
        }

        private void InitializeOptions(ITextBuffer textBuffer)
        {
            // Ideally we would initialize options based on Razor specific options in the context menu.
            // But since we don't have support for that yet, we will temporarily use the settings from Html.

            var textManager = _serviceProvider.GetService(typeof(SVsTextManager)) as IVsTextManager2;
            Assumes.Present(textManager);

            var langPrefs2 = new LANGPREFERENCES2[] { new LANGPREFERENCES2() { guidLang = HtmlLanguageServiceGuid } };
            if (VSConstants.S_OK == textManager.GetUserPreferences2(null, null, langPrefs2, null))
            {
                var insertSpaces = langPrefs2[0].fInsertTabs == 0;
                var tabSize = langPrefs2[0].uTabSize;

                var razorOptions = _editorOptionsFactory.GetOptions(textBuffer);
                razorOptions.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, insertSpaces);
                razorOptions.SetOptionValue(DefaultOptions.TabSizeOptionId, (int)tabSize);
            }
        }

        private void RazorGuestBuffer_Changed(object sender, TextContentChangedEventArgs args)
        {
            var replacePlaceholderChange = args.Changes.FirstOrDefault(
                c => c.OldText == LanguageServerConstants.CursorPlaceholderString && c.NewText == string.Empty);

            if (replacePlaceholderChange == null)
            {
                return;
            }

            if (!(sender is ITextBuffer buffer) ||
                !buffer.Properties.TryGetProperty<string>(FilePathPropertyKey, out var filePath))
            {
                return;
            }

            _editorService.MoveCaretToPosition(filePath, replacePlaceholderChange.NewPosition);
        }
    }
}
