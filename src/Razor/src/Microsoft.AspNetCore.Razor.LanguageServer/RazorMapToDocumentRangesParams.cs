// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using MediatR;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorMapToDocumentRangesParams : IRequest<RazorMapToDocumentRangesResponse>
    {
        public RazorLanguageKind Kind { get; set; }

        public Uri RazorDocumentUri { get; set; }

        public Range[] ProjectedRanges { get; set; }
    }
}