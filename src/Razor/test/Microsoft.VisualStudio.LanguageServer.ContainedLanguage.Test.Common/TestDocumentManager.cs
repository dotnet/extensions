// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    internal class TestDocumentManager : TrackingLSPDocumentManager
    {
        private readonly Dictionary<Uri, LSPDocumentSnapshot> _documents = new Dictionary<Uri, LSPDocumentSnapshot>();

        public override event EventHandler<LSPDocumentChangeEventArgs> Changed;

        public int UpdateVirtualDocumentCallCount { get; private set; }

        public override bool TryGetDocument(Uri uri, out LSPDocumentSnapshot lspDocumentSnapshot)
        {
            return _documents.TryGetValue(uri, out lspDocumentSnapshot);
        }

        public void AddDocument(Uri uri, LSPDocumentSnapshot documentSnapshot)
        {
            _documents.Add(uri, documentSnapshot);

            Changed?.Invoke(this, null);
        }

        public override void TrackDocument(ITextBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void UntrackDocument(ITextBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public override void UpdateVirtualDocument<TVirtualDocument>(Uri hostDocumentUri, IReadOnlyList<ITextChange> changes, int hostDocumentVersion)
        {
            UpdateVirtualDocumentCallCount++;
        }

        [Obsolete]
        public override void UpdateVirtualDocument<TVirtualDocument>(Uri hostDocumentUri, IReadOnlyList<ITextChange> changes, long hostDocumentVersion)
        {
            throw new NotImplementedException();
        }
    }
}
