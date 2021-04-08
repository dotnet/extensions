// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    /// <summary>
    /// The <see cref="VirtualDocumentFactory"/>'s purpose is to create a <see cref="VirtualDocument"/> for a given <see cref="ITextBuffer"/>.
    /// These <see cref="VirtualDocument"/>s are addressable via their <see cref="VirtualDocument.Uri"/>'s and represnt an embedded, addressable LSP
    /// document for a provided <see cref="ITextBuffer"/>.
    /// </summary>
    public abstract class VirtualDocumentFactory
    {
        /// <summary>
        /// Attempts to create a <see cref="VirtualDocument"/> for the provided <paramref name="hostDocumentBuffer"/>.
        /// </summary>
        /// <param name="hostDocumentBuffer">The top-level LSP document buffer.</param>
        /// <param name="virtualDocument">The resultant <see cref="VirtualDocument"/> for the top-level <paramref name="hostDocumentBuffer"/>.</param>
        /// <returns><c>true</c> if a <see cref="VirtualDocument"/> could be created, <c>false</c> otherwise. A result of <c>false</c> typically indicates
        /// that a <see cref="VirtualDocumentFactory"/> was not meant to be called for the given <paramref name="hostDocumentBuffer"/>.</returns>
        public abstract bool TryCreateFor(ITextBuffer hostDocumentBuffer, out VirtualDocument virtualDocument);
    }
}
