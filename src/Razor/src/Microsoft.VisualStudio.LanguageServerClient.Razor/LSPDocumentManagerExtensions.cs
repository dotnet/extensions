// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public static class LSPDocumentManagerExtensions
    {
        /// <summary>
        /// Retrieves the <see cref="LSPDocument"/> associated with the <paramref name="filePath"/> by converting it to a <see cref="Uri"/>.
        /// </summary>
        /// <param name="documentManager">The <see cref="LSPDocumentManager"/> to use.</param>
        /// <param name="filePath">The file path of the document to look up.</param>
        /// <param name="lspDocument">The resolved <see cref="LSPDocument"/>.</param>
        /// <returns><c>true</c> if the <see cref="LSPDocument"/> could be resolved for the given <paramref name="filePath"/>, <c>false</c> otherwise.</returns>
        public static bool TryGetDocument(this LSPDocumentManager documentManager, string filePath, out LSPDocument lspDocument)
        {
            if (documentManager is null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.StartsWith("/") && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                filePath = filePath.Substring(1);
            }

            var uri = new Uri(filePath, UriKind.Absolute);
            return documentManager.TryGetDocument(uri, out lspDocument);
        }
    }
}
