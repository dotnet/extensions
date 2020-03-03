// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public sealed class LSPDocumentChangeEventArgs : EventArgs
    {
        public LSPDocumentChangeEventArgs(LSPDocumentSnapshot old, LSPDocumentSnapshot @new, LSPDocumentChangeKind kind)
        {
            Old = old;
            New = @new;
            Kind = kind;
        }

        public LSPDocumentSnapshot Old { get; }

        public LSPDocumentSnapshot New { get; }

        public LSPDocumentChangeKind Kind { get; }
    }
}
