
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models
{
    internal class SemanticTokensEditParams : ITextDocumentIdentifierParams, IRequest<SemanticTokensOrSemanticTokensEdits?>
    {
        public string PreviousResultId { get; set; }

        public TextDocumentIdentifier TextDocument { get; set; }
    }
}
