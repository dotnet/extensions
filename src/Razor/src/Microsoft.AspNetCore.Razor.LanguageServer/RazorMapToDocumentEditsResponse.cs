// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorMapToDocumentEditsResponse
    {
        public TextEdit[] TextEdits { get; set; }

        public int? HostDocumentVersion { get; set; }
    }
}
