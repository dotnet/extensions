// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    /// <summary>
    /// The core purpose of a virtual document is to represent an embedded language LSP document that is addressable to make re-invoking
    /// LSP requests possible.
    ///
    /// For instance, when we want to invoke an LSP request on a virtual document we'll go through the <see cref="LSPDocumentManager"/>
    /// to locate the top level <see cref="LSPDocument"/>, find the <see cref="VirtualDocument"/> we care about, invoke the LSP request
    /// with the new <see cref="Uri"/>.
    /// </summary>
    public abstract class VirtualDocument
    {
        /// <summary>
        /// The address to use to refer to the current <see cref="VirtualDocument"/>.
        /// </summary>
        public abstract Uri Uri { get; }

        /// <summary>
        /// The host document version this virtual document is associated with.
        ///
        /// This can be <c>null</c> if the virtual document has not yet been initialized for the assocaited host document.
        /// </summary>
        public abstract long? HostDocumentSyncVersion { get; }
    }
}
