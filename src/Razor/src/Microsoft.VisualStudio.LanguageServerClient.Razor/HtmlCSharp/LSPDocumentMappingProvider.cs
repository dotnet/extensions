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
        public abstract Task<RazorMapToDocumentRangesResponse> MapToDocumentRangesAsync(RazorLanguageKind languageKind, Uri razorDocumentUri, Range[] projectedRanges, CancellationToken cancellationToken);

        public abstract Task<Location[]> RemapLocationsAsync(Location[] locations, CancellationToken cancellationToken);

        public abstract Task<TextEdit[]> RemapTextEditsAsync(Uri uri, TextEdit[] edits, CancellationToken cancellationToken);

        public abstract Task<TextEdit[]> RemapFormattedTextEditsAsync(Uri uri, TextEdit[] edits, FormattingOptions options, CancellationToken cancellationToken);

        public abstract Task<WorkspaceEdit> RemapWorkspaceEditAsync(WorkspaceEdit workspaceEdit, CancellationToken cancellationToken);
    }
}
