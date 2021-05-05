// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal abstract class LSPDiagnosticsTranslator
    {
        public abstract Task<RazorDiagnosticsResponse> TranslateAsync(
            RazorLanguageKind languageKind,
            Uri razorDocumentUri,
            Diagnostic[] diagnostics,
            CancellationToken cancellationToken);
    }
}
