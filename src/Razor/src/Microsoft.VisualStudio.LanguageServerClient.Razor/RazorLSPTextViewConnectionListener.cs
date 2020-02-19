// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [ContentType(RazorLSPContentTypeDefinition.Name)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [Export(typeof(ITextViewConnectionListener))]
    internal class RazorLSPTextViewConnectionListener : ITextViewConnectionListener
    {
        private readonly LSPDocumentManagerBase _lspDocumentManager;

        [ImportingConstructor]
        public RazorLSPTextViewConnectionListener(
            LSPDocumentManager lspDocumentManager)
        {
            if (lspDocumentManager is null)
            {
                throw new ArgumentNullException(nameof(lspDocumentManager));
            }

            _lspDocumentManager = lspDocumentManager as LSPDocumentManagerBase;

            Debug.Assert(_lspDocumentManager != null);
        }

        public void SubjectBuffersConnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
        {
            foreach (var textBuffer in subjectBuffers)
            {
                if (!textBuffer.IsRazorLSPBuffer())
                {
                    continue;
                }

                // This initializes the Razor LSP world and constructs C#/HTML buffers for the embedded language parts of the Razor document.
                _lspDocumentManager.TrackDocumentView(textBuffer, textView);
            }
        }

        public void SubjectBuffersDisconnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
        {
            foreach (var textBuffer in subjectBuffers)
            {
                if (!textBuffer.IsRazorLSPBuffer())
                {
                    continue;
                }

                // This initializes the Razor LSP world and constructs C#/HTML buffers for the embedded language parts of the Razor document.
                _lspDocumentManager.UntrackDocumentView(textBuffer, textView);
            }
        }
    }
}
