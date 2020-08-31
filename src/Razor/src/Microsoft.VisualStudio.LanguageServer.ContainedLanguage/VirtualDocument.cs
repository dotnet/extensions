// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public abstract class VirtualDocument : IDisposable
    {
        public abstract Uri Uri { get; }

        public abstract ITextBuffer TextBuffer { get; }

        public abstract VirtualDocumentSnapshot CurrentSnapshot { get; }

        public abstract int HostDocumentVersion { get; }

        [Obsolete]
        public abstract long? HostDocumentSyncVersion { get; }

        public abstract VirtualDocumentSnapshot Update(IReadOnlyList<ITextChange> changes, int hostDocumentVersion);

        [Obsolete]
        public abstract VirtualDocumentSnapshot Update(IReadOnlyList<ITextChange> changes, long hostDocumentVersion);

        public abstract void Dispose();
    }
}
