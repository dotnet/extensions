// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

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

        [ImportingConstructor]
        public RazorLSPTextViewConnectionListener(
            IVsEditorAdaptersFactoryService editorAdaptersFactory,
            LSPEditorFeatureDetector editorFeatureDetector)
        {
            if (editorAdaptersFactory is null)
            {
                throw new ArgumentNullException(nameof(editorAdaptersFactory));
            }

            if (editorFeatureDetector is null)
            {
                throw new ArgumentNullException(nameof(editorFeatureDetector));
            }

            _editorAdaptersFactory = editorAdaptersFactory;
            _editorFeatureDetector = editorFeatureDetector;
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
        }

        public void SubjectBuffersDisconnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
        {
            // When the TextView goes away so does the filter.  No need to do anything more.
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
    }
}
