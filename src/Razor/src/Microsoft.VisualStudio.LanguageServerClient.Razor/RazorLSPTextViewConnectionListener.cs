// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using MediatR;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using DidChangeConfigurationParams = OmniSharp.Extensions.LanguageServer.Protocol.Models.DidChangeConfigurationParams;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    /// <summary>
    /// The entire purpose of this class is to enable us to apply our TextView filter to Razor text views in order to work around lacking debugging support in the
    /// LSP platform for default language servers. Ultimately this enables us to provide "hover" results 
    /// </summary>
    [Export(typeof(ITextViewConnectionListener))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [ContentType(RazorLSPConstants.RazorLSPContentTypeName)]
    internal class RazorLSPTextViewConnectionListener : ITextViewConnectionListener
    {
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactory;
        private readonly LSPEditorFeatureDetector _editorFeatureDetector;
        private readonly IEditorOptionsFactoryService _editorOptionsFactory;
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly RazorLSPClientOptionsMonitor _clientOptionsMonitor;
        private readonly IVsTextManager2 _textManager;

        /// <summary>
        /// Protects concurrent modifications to _activeTextViews and _textBuffer's
        /// property bag.
        /// </summary>
        private readonly object _lock = new();

        #region protected by _lock
        private readonly List<ITextView> _activeTextViews = new();

        private ITextBuffer _textBuffer;
        #endregion

        [ImportingConstructor]
        public RazorLSPTextViewConnectionListener(
            IVsEditorAdaptersFactoryService editorAdaptersFactory,
            LSPEditorFeatureDetector editorFeatureDetector,
            IEditorOptionsFactoryService editorOptionsFactory,
            LSPRequestInvoker requestInvoker,
            RazorLSPClientOptionsMonitor clientOptionsMonitor,
            SVsServiceProvider serviceProvider)
        {
            if (editorAdaptersFactory is null)
            {
                throw new ArgumentNullException(nameof(editorAdaptersFactory));
            }

            if (editorFeatureDetector is null)
            {
                throw new ArgumentNullException(nameof(editorFeatureDetector));
            }

            if (editorOptionsFactory is null)
            {
                throw new ArgumentNullException(nameof(editorOptionsFactory));
            }

            if (requestInvoker is null)
            {
                throw new ArgumentNullException(nameof(requestInvoker));
            }

            if (clientOptionsMonitor is null)
            {
                throw new ArgumentNullException(nameof(clientOptionsMonitor));
            }

            if (serviceProvider is null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _editorAdaptersFactory = editorAdaptersFactory;
            _editorFeatureDetector = editorFeatureDetector;
            _editorOptionsFactory = editorOptionsFactory;
            _requestInvoker = requestInvoker;
            _clientOptionsMonitor = clientOptionsMonitor;
            _textManager = serviceProvider.GetService(typeof(SVsTextManager)) as IVsTextManager2;

            Assumes.Present(_textManager);
        }

        public void SubjectBuffersConnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
        {
            if (textView is null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            var vsTextView = _editorAdaptersFactory.GetViewAdapter(textView);

            // In remote client scenarios there's a custom language service applied to buffers in order to enable delegation of interactions.
            // Because of this we don't want to break that experience so we ensure not to "set" a langauge service for remote clients.
            if (!_editorFeatureDetector.IsRemoteClient())
            {
                vsTextView.GetBuffer(out var vsBuffer);
                vsBuffer.SetLanguageServiceID(RazorLSPConstants.RazorLanguageServiceGuid);
            }

            RazorLSPTextViewFilter.CreateAndRegister(vsTextView);

            if (!textView.TextBuffer.IsRazorLSPBuffer())
            {
                return;
            }

            lock (_lock)
            {
                _activeTextViews.Add(textView);

                // Initialize the user's options and start listening for changes.
                // We only want to attach the option changed event once so we don't receive multiple
                // notifications if there is more than one TextView active.
                if (!textView.TextBuffer.Properties.ContainsProperty(typeof(RazorEditorOptionsTracker)))
                {
                    // We assume there is ever only one TextBuffer at a time and thus all active
                    // TextViews have the same TextBuffer.
                    _textBuffer = textView.TextBuffer;

                    var bufferOptions = _editorOptionsFactory.GetOptions(_textBuffer);
                    var viewOptions = _editorOptionsFactory.GetOptions(textView);

                    Assumes.Present(bufferOptions);
                    Assumes.Present(viewOptions);

                    // All TextViews share the same options, so we only need to listen to changes for one.
                    // We need to keep track of and update both the TextView and TextBuffer options. Updating
                    // the TextView's options is necessary so 'SPC'/'TABS' in the bottom right corner of the
                    // view displays the right setting. Updating the TextBuffer is necessary since it's where
                    // LSP pulls settings from when sending us requests.
                    var optionsTracker = new RazorEditorOptionsTracker(TrackedView: textView, viewOptions, bufferOptions);
                    _textBuffer.Properties[typeof(RazorEditorOptionsTracker)] = optionsTracker;

                    // A change in Tools->Options settings only kicks off an options changed event in the view
                    // and not the buffer, i.e. even if we listened for TextBuffer option changes, we would never
                    // be notified. As a workaround, we listen purely for TextView changes, and update the
                    // TextBuffer options in the TextView listener as well.
                    RazorOptions_OptionChanged(null, null);
                    viewOptions.OptionChanged += RazorOptions_OptionChanged;
                }
            }
        }

        public void SubjectBuffersDisconnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
        {
            // When the TextView goes away so does the filter.  No need to do anything more.
            // However, we do need to detach from listening for option changes to avoid leaking.
            // We should switch to listening to a different TextView if the one we're listening
            // to is disconnected.
            Assumes.NotNull(_textBuffer);

            if (!textView.TextBuffer.IsRazorLSPBuffer())
            {
                return;
            }

            lock (_lock)
            {
                _activeTextViews.Remove(textView);

                // Is the tracked TextView where we listen for option changes the one being disconnected?
                // If so, see if another view is available.
                if (_textBuffer.Properties.TryGetProperty(
                    typeof(RazorEditorOptionsTracker), out RazorEditorOptionsTracker optionsTracker) &&
                    optionsTracker.TrackedView == textView)
                {
                    _textBuffer.Properties.RemoveProperty(typeof(RazorEditorOptionsTracker));
                    optionsTracker.ViewOptions.OptionChanged -= RazorOptions_OptionChanged;

                    // If there's another text view we can use to listen for options, start tracking it.
                    if (_activeTextViews.Count != 0)
                    {
                        var newTrackedView = _activeTextViews[0];
                        var newViewOptions = _editorOptionsFactory.GetOptions(newTrackedView);
                        Assumes.Present(newViewOptions);

                        // We assume the TextViews all have the same TextBuffer, so we can reuse the
                        // buffer options from the old TextView.
                        var newOptionsTracker = new RazorEditorOptionsTracker(
                            newTrackedView, newViewOptions, optionsTracker.BufferOptions);
                        _textBuffer.Properties[typeof(RazorEditorOptionsTracker)] = newOptionsTracker;

                        newViewOptions.OptionChanged += RazorOptions_OptionChanged;
                    }
                }
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void RazorOptions_OptionChanged(object sender, EditorOptionChangedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            Assumes.NotNull(_textBuffer);

            if (!_textBuffer.Properties.TryGetProperty(typeof(RazorEditorOptionsTracker), out RazorEditorOptionsTracker optionsTracker))
            {
                return;
            }

            // Retrieve current space/tabs settings from from Tools->Options.
            var settings = GetRazorEditorOptions(_textManager);

            // Update settings in the actual editor.
            // We need to update both the TextView and TextBuffer options. Updating the TextView is necessary
            // so 'SPC'/'TABS' in the bottom right corner of the view displays the right setting. Updating the
            // TextBuffer is necessary since it's where LSP pulls settings from when sending us requests.
            optionsTracker.ViewOptions.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, !settings.IndentWithTabs);
            optionsTracker.ViewOptions.SetOptionValue(DefaultOptions.TabSizeOptionId, settings.IndentSize);

            optionsTracker.BufferOptions.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, !settings.IndentWithTabs);
            optionsTracker.BufferOptions.SetOptionValue(DefaultOptions.TabSizeOptionId, settings.IndentSize);

            // Keep track of accurate settings on the client side so we can easily retrieve the
            // options later when the server sends us a workspace/configuration request.
            _clientOptionsMonitor.UpdateOptions(settings);

            try
            {
                // Make sure the server updates the settings on their side by sending a
                // workspace/didChangeConfiguration request. This notifies the server that the user's
                // settings have changed. The server will then query the client's options monitor (already
                // updated via the line above) by sending a workspace/configuration request.
                // NOTE: This flow uses polyfilling because VS doesn't yet support workspace configuration
                // updates. Once they do, we can get rid of this extra logic.
                await _requestInvoker.ReinvokeRequestOnServerAsync<DidChangeConfigurationParams, Unit>(
                    Methods.WorkspaceDidChangeConfigurationName,
                    RazorLSPConstants.RazorLSPContentTypeName,
                    new DidChangeConfigurationParams(),
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                // This may happen if the TextView finishes attaching before the Razor language server is
                // initialized. Ideally, the platform should be queuing requests until the language server
                // is ready. However, we should catch any exceptions here just in case since VS will crash
                // if an exception is hit in this method.
                Debug.Fail($"Error executing workspace/didChangeConfiguration request: {ex.Message}");
            }
        }

        private static EditorSettings GetRazorEditorOptions(IVsTextManager2 textManager)
        {
            var insertSpaces = RazorLSPOptions.Default.InsertSpaces;
            var tabSize = RazorLSPOptions.Default.TabSize;

            var langPrefs2 = new LANGPREFERENCES2[] { new LANGPREFERENCES2() { guidLang = RazorLSPConstants.RazorLanguageServiceGuid } };
            if (VSConstants.S_OK == textManager.GetUserPreferences2(null, null, langPrefs2, null))
            {
                insertSpaces = langPrefs2[0].fInsertTabs == 0;
                tabSize = (int)langPrefs2[0].uTabSize;
            }

            return new EditorSettings(indentWithTabs: !insertSpaces, tabSize);
        }

        private class RazorLSPTextViewFilter : IOleCommandTarget, IVsTextViewFilter
        {
            private RazorLSPTextViewFilter()
            {
            }

            private IOleCommandTarget Next { get; set; }

            public static void CreateAndRegister(IVsTextView textView)
            {
                var viewFilter = new RazorLSPTextViewFilter();
                textView.AddCommandFilter(viewFilter, out var next);

                viewFilter.Next = next;
            }

            public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
            {
                var queryResult = Next.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
                return queryResult;
            }

            public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
            {
                var execResult = Next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                return execResult;
            }

            public int GetWordExtent(int iLine, int iIndex, uint dwFlags, TextSpan[] pSpan) => VSConstants.E_NOTIMPL;

            public int GetDataTipText(TextSpan[] pSpan, out string pbstrText)
            {
                pbstrText = null;
                return VSConstants.E_NOTIMPL;
            }

            public int GetPairExtents(int iLine, int iIndex, TextSpan[] pSpan) => VSConstants.E_NOTIMPL;
        }

        private record RazorEditorOptionsTracker(ITextView TrackedView, IEditorOptions ViewOptions, IEditorOptions BufferOptions);
    }
}
