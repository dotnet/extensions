// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using OmniSharpPosition = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [Export(typeof(LSPProjectionProvider))]
    internal class DefaultLSPProjectionProvider : LSPProjectionProvider
    {
        private readonly int UndefinedDocumentVersion = -1;
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPDocumentSynchronizer _documentSynchronizer;
        private readonly RazorLogger _logger;

        [ImportingConstructor]
        public DefaultLSPProjectionProvider(
            LSPRequestInvoker requestInvoker,
            LSPDocumentSynchronizer documentSynchronizer,
            RazorLogger logger)
        {
            _requestInvoker = requestInvoker;
            _documentSynchronizer = documentSynchronizer;
            _logger = logger;
        }

        public override async Task<ProjectionResult> GetProjectionAsync(LSPDocumentSnapshot documentSnapshot, Position position, CancellationToken cancellationToken)
        {
            if (documentSnapshot is null)
            {
                throw new ArgumentNullException(nameof(documentSnapshot));
            }

            if (position is null)
            {
                throw new ArgumentNullException(nameof(position));
            }

            var languageQueryParams = new RazorLanguageQueryParams()
            {
                Position = new OmniSharpPosition(position.Line, position.Character),
                Uri = documentSnapshot.Uri
            };

            var languageResponse = await _requestInvoker.RequestServerAsync<RazorLanguageQueryParams, RazorLanguageQueryResponse>(
                LanguageServerConstants.RazorLanguageQueryEndpoint,
                LanguageServerKind.Razor,
                languageQueryParams,
                cancellationToken);

            VirtualDocumentSnapshot virtualDocument;
            if (languageResponse.Kind == RazorLanguageKind.CSharp &&
                documentSnapshot.TryGetVirtualDocument<CSharpVirtualDocumentSnapshot>(out var csharpDoc))
            {
                virtualDocument = csharpDoc;
            }
            else if (languageResponse.Kind == RazorLanguageKind.Html &&
                documentSnapshot.TryGetVirtualDocument<HtmlVirtualDocumentSnapshot>(out var htmlDoc))
            {
                virtualDocument = htmlDoc;
            }
            else
            {
                return null;
            }

            if (languageResponse.HostDocumentVersion == UndefinedDocumentVersion)
            {
                // There should always be a document version attached to an open document.
                // Log it and move on as if it was synchronized.
                _logger.LogVerbose($"Could not find a document version associated with the document '{documentSnapshot.Uri}'");
            }
            else
            {
                var synchronized = await _documentSynchronizer.TrySynchronizeVirtualDocumentAsync(documentSnapshot, virtualDocument, cancellationToken);
                if (!synchronized)
                {
                    // Could not synchronize
                    return null;
                }
            }

            var result = new ProjectionResult()
            {
                Uri = virtualDocument.Uri,
                Position = new Position((int)languageResponse.Position.Line, (int)languageResponse.Position.Character),
                LanguageKind = languageResponse.Kind,
            };

            return result;
        }
    }
}
