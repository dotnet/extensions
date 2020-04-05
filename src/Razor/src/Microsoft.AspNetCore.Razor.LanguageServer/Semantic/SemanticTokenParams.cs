// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using MediatR;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    public class SemanticTokenParams : IRequest<SemanticTokens>
    {
        public RazorLanguageKind Kind { get; set; }
        public Uri RazorDocumentUri { get; set; }
    }
}
