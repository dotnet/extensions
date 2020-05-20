// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal sealed class RazorLSPContentTypeDefinition
    {
        /// <summary>
        /// Exports the Razor LSP content type
        /// </summary>
        [Export]
        [Name(RazorLSPConstants.RazorLSPContentTypeName)]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        public ContentTypeDefinition RazorLSPContentType { get; set; }

        // We can't associate the Razor LSP content type with the above file extensions because there's already a content type
        // associated with them. Instead, we utilize our RazorEditorFactory to assign the RazorLSPContentType to .razor/.cshtml
        // files.
    }
}
