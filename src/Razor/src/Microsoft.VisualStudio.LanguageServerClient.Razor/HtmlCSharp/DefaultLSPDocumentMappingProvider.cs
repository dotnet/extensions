// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.LanguageServer.Protocol;

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

        public async override Task<RazorMapToDocumentRangeResponse> MapToDocumentRangeAsync(RazorLanguageKind languageKind, Uri razorDocumentUri, Range projectedRange, CancellationToken cancellationToken)
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
                ProjectedRange = new Range()
                {
                    Start = new Position(projectedRange.Start.Line, projectedRange.Start.Character),
                    End = new Position(projectedRange.End.Line, projectedRange.End.Character)
                }
            };

            var documentMappingResponse = await _requestInvoker.CustomRequestServerAsync<RazorMapToDocumentRangeParams, RazorMapToDocumentRangeResponse>(
                LanguageServerConstants.RazorMapToDocumentRangeEndpoint,
                LanguageServerKind.Razor,
                mapToDocumentRangeParams,
                cancellationToken).ConfigureAwait(false);

            return documentMappingResponse;
        }
    }
}
