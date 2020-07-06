// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public class TestVirtualDocumentSnapshot : VirtualDocumentSnapshot
    {
        private long? _hostDocumentSyncVersion;

        public TestVirtualDocumentSnapshot(Uri uri, long? hostDocumentVersion) : this(uri, hostDocumentVersion, snapshot: null)
        {
        }

        public TestVirtualDocumentSnapshot(Uri uri, long? hostDocumentVersion, ITextSnapshot snapshot)
        {
            Uri = uri;
            _hostDocumentSyncVersion = hostDocumentVersion;
            Snapshot = snapshot;
        }

        public override Uri Uri { get; }

        public override ITextSnapshot Snapshot { get; }

        public override long? HostDocumentSyncVersion => _hostDocumentSyncVersion;

        public TestVirtualDocumentSnapshot Fork(int hostDocumentVersion) => new TestVirtualDocumentSnapshot(Uri, hostDocumentVersion);
    }
}
