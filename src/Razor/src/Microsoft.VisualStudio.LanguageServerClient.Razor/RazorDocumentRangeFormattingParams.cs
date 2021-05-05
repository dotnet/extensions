// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal class RazorDocumentRangeFormattingParams
    {
        public RazorLanguageKind Kind { get; set; }

        public string HostDocumentFilePath { get; set; }

        public Range ProjectedRange { get; set; }

        public FormattingOptions Options { get; set; }
    }
}
