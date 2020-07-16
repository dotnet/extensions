// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public sealed class LSPDocumentChangeEventArgs : EventArgs
    {
        public LSPDocumentChangeEventArgs(LSPDocumentSnapshot old, LSPDocumentSnapshot @new, LSPDocumentChangeKind kind)
            : this(old, @new, virtualOld: null, virtualNew: null, kind)
        {
        }

        public LSPDocumentChangeEventArgs(
            LSPDocumentSnapshot old,
            LSPDocumentSnapshot @new,
            VirtualDocumentSnapshot virtualOld,
            VirtualDocumentSnapshot virtualNew,
            LSPDocumentChangeKind kind)
        {
            Old = old;
            New = @new;
            VirtualOld = virtualOld;
            VirtualNew = virtualNew;
            Kind = kind;
        }

        public LSPDocumentSnapshot Old { get; }

        public LSPDocumentSnapshot New { get; }

        public VirtualDocumentSnapshot VirtualOld { get; }

        public VirtualDocumentSnapshot VirtualNew { get; }

        public LSPDocumentChangeKind Kind { get; }
    }
}
