// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    internal class TestVirtualDocument : VirtualDocumentBase<TestVirtualDocumentSnapshot>
    {
        public TestVirtualDocument(Uri uri, ITextBuffer textBuffer) : base(uri, textBuffer)
        {
        }

        protected override TestVirtualDocumentSnapshot GetUpdatedSnapshot() => new TestVirtualDocumentSnapshot(Uri, HostDocumentVersion, TextBuffer.CurrentSnapshot);
    }
}
