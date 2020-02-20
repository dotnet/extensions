// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    /// <summary>
    /// An <see cref="LSPDocument"/> represents a top-level document that has embedded languages in it. For instance a Razor document has embedded
    /// C# and HTML. The purpose of the <see cref="LSPDocument"/> is to provide a mechanism through which consumers can address a given document
    /// via the <see cref="Uri"/> and based on calculated information such as "what language is being operated on?" extract virtual documents
    /// from the <see cref="VirtualDocuments"/> list in an effort to re-invoke LSP requests.
    /// </summary>
    public abstract class LSPDocument
    {
        /// <summary>
        /// The address to use to refer to the current <see cref="LSPDocument"/>.
        /// </summary>
        public abstract Uri Uri { get; }

        /// <summary>
        /// A collection of <see cref="VirtualDocument"/>s that represent the embedded documents for the top level <see cref="LSPDocument"/>.
        /// </summary>
        public abstract IReadOnlyList<VirtualDocument> VirtualDocuments { get; }

        /// <summary>
        /// Retrieves a virtual document of the provided <typeparamref name="TDocument"/> type.
        /// </summary>
        /// <typeparam name="TDocument">The type of <see cref="VirtualDocument"/> to find.</typeparam>
        /// <param name="virtualDocument">The virtual document of the provided <typeparamref name="TDocument"/> type.</param>
        /// <returns><c>true</c> if <see cref="VirtualDocuments"/> contains a <see cref="VirtualDocument"/> of the provided <typeparamref name="TDocument"/> type; <c>false</c> otherwise.</returns>
        public bool TryGetVirtualDocument<TDocument>(out TDocument virtualDocument) where TDocument : VirtualDocument
        {
            for (var i = 0; i < VirtualDocuments.Count; i++)
            {
                if (VirtualDocuments[i] is TDocument actualVirtualDocument)
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
