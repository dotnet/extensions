// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal class RazorMapToDocumentRangeParams
    {
        public RazorLanguageKind Kind { get; set; }

        public Uri RazorDocumentUri { get; set; }

        public Range ProjectedRange { get; set; }
    }
}
