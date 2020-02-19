// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public abstract class VirtualDocumentFactory
    {
        public abstract bool TryCreateFor(ITextBuffer hostDocumentBuffer, out VirtualDocument virtualDocument);
    }
}
