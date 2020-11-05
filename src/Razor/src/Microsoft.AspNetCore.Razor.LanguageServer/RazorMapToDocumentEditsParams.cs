// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorMapToDocumentEditsParams : IRequest<RazorMapToDocumentEditsResponse>
    {
        public RazorLanguageKind Kind { get; set; }

        public Uri RazorDocumentUri { get; set; }

        public TextEdit[] ProjectedTextEdits { get; set; }

        public bool ShouldFormat { get; set; }

        public FormattingOptions FormattingOptions { get; set; }
    }
}
