// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using OmniSharpPosition = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;
using OmniSharpRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [Export(typeof(LSPDocumentMappingProvider))]
    internal class DefaultLSPDocumentMappingProvider : LSPDocumentMappingProvider
    {
        private readonly LSPRequestInvoker _requestInvoker;

        [ImportingConstructor]
        public DefaultLSPDocumentMappingProvider(LSPRequestInvoker requestInvoker)
        {
            if (requestInvoker is null)
            {
                throw new ArgumentNullException(nameof(requestInvoker));
            }

            _requestInvoker = requestInvoker;
        }

        public async override Task<MappingResult> MapToDocumentRangeAsync(RazorLanguageKind languageKind, Uri razorDocumentUri, Range projectedRange, CancellationToken cancellationToken)
        {
            if (razorDocumentUri is null)
            {
                throw new ArgumentNullException(nameof(razorDocumentUri));
            }

            if (projectedRange is null)
            {
                throw new ArgumentNullException(nameof(projectedRange));
            }

            var mapToDocumentRangeParams = new RazorMapToDocumentRangeParams()
            {
                Kind = languageKind,
                RazorDocumentUri = razorDocumentUri,
                ProjectedRange = new OmniSharpRange(
                    new OmniSharpPosition(projectedRange.Start.Line, projectedRange.Start.Character),
                    new OmniSharpPosition(projectedRange.End.Line, projectedRange.End.Character))
            };

            var documentMappingResponse = await _requestInvoker.RequestServerAsync<RazorMapToDocumentRangeParams, RazorMapToDocumentRangeResponse>(
                LanguageServerConstants.RazorMapToDocumentRangeEndpoint,
                LanguageServerKind.Razor,
                mapToDocumentRangeParams,
                cancellationToken).ConfigureAwait(false);

            var mappingResult = new MappingResult()
            {
                Range = new Range()
                {
                    Start = new Position((int)documentMappingResponse.Range.Start.Line, (int)documentMappingResponse.Range.Start.Character),
                    End = new Position((int)documentMappingResponse.Range.End.Line, (int)documentMappingResponse.Range.End.Character),
                },
                HostDocumentVersion = documentMappingResponse.HostDocumentVersion
            };

            return mappingResult;
        }
    }
}
