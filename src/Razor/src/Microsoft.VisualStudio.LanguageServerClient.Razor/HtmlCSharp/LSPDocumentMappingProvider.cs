// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal abstract class LSPDocumentMappingProvider
    {
        public abstract Task<RazorMapToDocumentRangeResponse> MapToDocumentRangeAsync(RazorLanguageKind languageKind, Uri razorDocumentUri, Range projectedRange, CancellationToken cancellationToken);

        public abstract Task<WorkspaceEdit> RemapWorkspaceEditAsync(WorkspaceEdit workspaceEdit, CancellationToken cancellationToken);
    }
}
