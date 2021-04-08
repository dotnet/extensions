// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    // Note: This type should be kept in sync with the one in VisualStudio.LanguageServerClient assembly.
    internal class RazorDiagnosticsParams : IRequest<RazorDiagnosticsResponse>
    {
        public RazorLanguageKind Kind { get; set; }

        public Uri RazorDocumentUri { get; set; }

        public Diagnostic[] Diagnostics { get; set; }
    }
}
