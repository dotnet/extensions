// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    /// <summary>
    /// There are two primary purposes of the <see cref="LSPDocumentManager"/>.
    ///
    /// 1. Track <see cref="LSPDocument"/> lifecycles.
    /// 2. Allow known <see cref="LSPDocument"/>s to be looked up via <see cref="TryGetDocument(Uri, out LSPDocument)"/>.
    /// </summary>
    public abstract class LSPDocumentManager
    {
        /// <summary>
        /// Retrieves the <see cref="LSPDocument"/> associated with the <paramref name="uri"/>.
        /// </summary>
        /// <param name="uri">The address of the document to look up.</param>
        /// <param name="lspDocument">The resolved <see cref="LSPDocument"/>.</param>
        /// <returns><c>true</c> if the <see cref="LSPDocument"/> could be resolved for the given <paramref name="uri"/>, <c>false</c> otherwise.</returns>
        public abstract bool TryGetDocument(Uri uri, out LSPDocument lspDocument);
    }
}
