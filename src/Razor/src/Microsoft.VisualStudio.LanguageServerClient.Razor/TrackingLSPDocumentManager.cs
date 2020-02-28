// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal abstract class TrackingLSPDocumentManager : LSPDocumentManager
    {
        public abstract void TrackDocument(ITextBuffer buffer);

        public abstract void UntrackDocument(ITextBuffer buffer);
    }
}
