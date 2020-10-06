// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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

        [ImportingConstructor]
        public RazorLSPTextViewConnectionListener(IVsEditorAdaptersFactoryService editorAdaptersFactory)
        {
            if (editorAdaptersFactory is null)
            {
                throw new ArgumentNullException(nameof(editorAdaptersFactory));
            }

            _editorAdaptersFactory = editorAdaptersFactory;
        }

        public void SubjectBuffersConnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
        {
            if (textView is null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            var vsTextView = _editorAdaptersFactory.GetViewAdapter(textView);

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
