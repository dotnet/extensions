// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class RazorDocumentRangeFormattingParams : IRequest<RazorDocumentRangeFormattingResponse>
    {
        public RazorLanguageKind Kind { get; set; }

        public string HostDocumentFilePath { get; set; }

        public Range ProjectedRange { get; set; }

        public FormattingOptions Options { get; set; }
    }
}
