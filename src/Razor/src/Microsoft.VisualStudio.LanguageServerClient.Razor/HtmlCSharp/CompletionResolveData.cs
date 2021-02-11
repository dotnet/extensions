// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal class CompletionResolveData
    {
        public Uri HostDocumentUri { get; set; }

        public Uri ProjectedDocumentUri { get; set; }

        public LanguageServerKind LanguageServerKind { get; set; }

        public object OriginalData { get; set; }
    }
}
