// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Test;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public class TestLSPDocumentSnapshot : LSPDocumentSnapshot
    {
        public TestLSPDocumentSnapshot(Uri uri, int version, params VirtualDocumentSnapshot[] virtualDocuments)
            : this(uri, version, snapshotContent: "Hello World", virtualDocuments)
        {
        }

        public TestLSPDocumentSnapshot(Uri uri, int version, string snapshotContent, params VirtualDocumentSnapshot[] virtualDocuments)
        {
            Uri = uri;
            Version = version;
            VirtualDocuments = virtualDocuments;
            var snapshot = new StringTextSnapshot(snapshotContent);
            _ = new TestTextBuffer(snapshot);
            Snapshot = snapshot;
        }

        public override int Version { get; }

        public override Uri Uri { get; }

        public override ITextSnapshot Snapshot { get; }

        public override IReadOnlyList<VirtualDocumentSnapshot> VirtualDocuments { get; }

        public TestLSPDocumentSnapshot Fork(int version, params VirtualDocumentSnapshot[] virtualDocuments) => new TestLSPDocumentSnapshot(Uri, version, virtualDocuments);
    }
}
