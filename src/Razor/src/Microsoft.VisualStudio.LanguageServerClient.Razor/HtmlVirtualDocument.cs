// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal class HtmlVirtualDocument : VirtualDocumentBase<HtmlVirtualDocumentSnapshot>
    {
        public HtmlVirtualDocument(Uri uri, ITextBuffer textBuffer) : base(uri, textBuffer)
        {
        }

        protected override HtmlVirtualDocumentSnapshot GetUpdatedSnapshot() => new HtmlVirtualDocumentSnapshot(Uri, TextBuffer.CurrentSnapshot, HostDocumentVersion);
    }
}
