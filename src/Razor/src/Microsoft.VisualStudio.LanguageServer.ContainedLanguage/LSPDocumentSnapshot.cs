// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public abstract class LSPDocumentSnapshot
    {
        public abstract int Version { get; }

        public abstract Uri Uri { get; }

        public abstract ITextSnapshot Snapshot { get; }

        public abstract IReadOnlyList<VirtualDocumentSnapshot> VirtualDocuments { get; }

        public bool TryGetVirtualDocument<TVirtualDocument>(out TVirtualDocument virtualDocument) where TVirtualDocument : VirtualDocumentSnapshot
        {
            for (var i = 0; i < VirtualDocuments.Count; i++)
            {
                if (VirtualDocuments[i] is TVirtualDocument actualVirtualDocument)
                {
                    virtualDocument = actualVirtualDocument;
                    return true;
                }
            }

            virtualDocument = null;
            return false;
        }
    }
}
