// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal class RazorMapToDocumentEditsResponse
    {
        public TextEdit[] TextEdits { get; set; }

        public long HostDocumentVersion { get; set; }
    }
}
