// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public static class LSPDocumentManagerExtensions
    {
        public static bool TryGetDocument(this LSPDocumentManager documentManager, string filePath, out LSPDocumentSnapshot lspDocumentSnapshot)
        {
            if (documentManager is null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.StartsWith("/", StringComparison.Ordinal) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                filePath = filePath.Substring(1);
            }

            var uri = new Uri(filePath, UriKind.Absolute);
            return documentManager.TryGetDocument(uri, out lspDocumentSnapshot);
        }
    }
}
