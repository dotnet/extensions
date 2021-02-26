// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [Export(typeof(LSPProjectionProvider))]
    internal class DefaultLSPProjectionProvider : LSPProjectionProvider
    {
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPDocumentSynchronizer _documentSynchronizer;
        private readonly RazorLogger _logger;
        private Func<RazorLanguageQueryParams, CancellationToken, Task<RazorLanguageQueryResponse>> _inProcLanguageQueryMethod;

        [ImportingConstructor]
        public DefaultLSPProjectionProvider(
            LSPRequestInvoker requestInvoker,
            LSPDocumentSynchronizer documentSynchronizer,
            RazorLogger logger)
        {
            if (requestInvoker is null)
            {
                throw new ArgumentNullException(nameof(requestInvoker));
            }

            if (documentSynchronizer is null)
            {
                throw new ArgumentNullException(nameof(documentSynchronizer));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _requestInvoker = requestInvoker;
            _documentSynchronizer = documentSynchronizer;
            _logger = logger;
        }

        public void UseInProcLanguageQueries(Func<RazorLanguageQueryParams, CancellationToken, Task<RazorLanguageQueryResponse>> inProcLanguageQueryMethod)
        {
            if (inProcLanguageQueryMethod is null)
            {
                throw new ArgumentNullException(nameof(inProcLanguageQueryMethod));
            }

            _inProcLanguageQueryMethod = inProcLanguageQueryMethod;
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
                Position = position,
                Uri = documentSnapshot.Uri,
            };

            RazorLanguageQueryResponse languageResponse = null;
            if (_inProcLanguageQueryMethod != null)
            {
                languageResponse = await _inProcLanguageQueryMethod.Invoke(languageQueryParams, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                languageResponse = await _requestInvoker.ReinvokeRequestOnServerAsync<RazorLanguageQueryParams, RazorLanguageQueryResponse>(
                    LanguageServerConstants.RazorLanguageQueryEndpoint,
                    RazorLSPConstants.RazorLSPContentTypeName,
                    languageQueryParams,
                    cancellationToken).ConfigureAwait(false);
            }

            if (languageResponse == null)
            {
                // The language server is still being spun up. Could not resolve the projection.
                return null;
            }

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

            if (languageResponse.HostDocumentVersion is null)
            {
                // There should always be a document version attached to an open document.
                // Log it and move on as if it was synchronized.
                _logger.LogVerbose($"Could not find a document version associated with the document '{documentSnapshot.Uri}'");
            }
            else
            {
                var synchronized = await _documentSynchronizer.TrySynchronizeVirtualDocumentAsync(documentSnapshot.Version, virtualDocument, cancellationToken).ConfigureAwait(false);
                if (!synchronized)
                {
                    // Could not synchronize
                    return null;
                }
            }

            var result = new ProjectionResult()
            {
                Uri = virtualDocument.Uri,
                Position = languageResponse.Position,
                PositionIndex = languageResponse.PositionIndex,
                LanguageKind = languageResponse.Kind,
                HostDocumentVersion = languageResponse.HostDocumentVersion
            };

            return result;
        }
    }
}
