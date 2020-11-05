// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#pragma warning disable CS0618
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal abstract class RazorSemanticTokensInfoService
    {
        public abstract Task<SemanticTokens> GetSemanticTokensAsync(DocumentSnapshot codeDocument, TextDocumentIdentifier textDocumentIdentifier, CancellationToken cancellationToken);

        public abstract Task<SemanticTokens> GetSemanticTokensAsync(DocumentSnapshot codeDocument, TextDocumentIdentifier textDocumentIdentifier, Range range, CancellationToken cancellationToken);

        public abstract Task<SemanticTokensFullOrDelta> GetSemanticTokensEditsAsync(DocumentSnapshot codeDocument, TextDocumentIdentifier textDocumentIdentifier, string previousId, CancellationToken cancellationToken);
    }
}
