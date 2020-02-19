// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal class DefaultLSPDocument : LSPDocument
    {
        public DefaultLSPDocument(Uri uri, IReadOnlyList<VirtualDocument> virtualDocuments)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (virtualDocuments is null)
            {
                throw new ArgumentNullException(nameof(virtualDocuments));
            }

            Uri = uri;
            VirtualDocuments = virtualDocuments;
        }

        public override Uri Uri { get; }

        public override IReadOnlyList<VirtualDocument> VirtualDocuments { get; }
    }
}
