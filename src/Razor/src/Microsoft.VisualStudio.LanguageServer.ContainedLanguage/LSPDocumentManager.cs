// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public abstract class LSPDocumentManager
    {
        public abstract event EventHandler<LSPDocumentChangeEventArgs> Changed;

        public abstract bool TryGetDocument(Uri uri, out LSPDocumentSnapshot lspDocumentSnapshot);
    }
}
