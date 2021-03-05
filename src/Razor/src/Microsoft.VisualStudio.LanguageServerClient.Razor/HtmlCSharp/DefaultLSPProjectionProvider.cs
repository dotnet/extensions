// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServerClient.Razor.Logging;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [Export(typeof(LSPProjectionProvider))]
    internal class DefaultLSPProjectionProvider : LSPProjectionProvider
    {
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPDocumentSynchronizer _documentSynchronizer;
        private readonly RazorLogger _activityLogger;
        private readonly HTMLCSharpLanguageServerLogHubLoggerProvider _loggerProvider;

        private ILogger _logHubLogger = null;

        [ImportingConstructor]
        public DefaultLSPProjectionProvider(
            LSPRequestInvoker requestInvoker,
            LSPDocumentSynchronizer documentSynchronizer,
            RazorLogger razorLogger,
            HTMLCSharpLanguageServerLogHubLoggerProvider loggerProvider)
        {
            if (requestInvoker is null)
            {
                throw new ArgumentNullException(nameof(requestInvoker));
            }

            if (documentSynchronizer is null)
            {
                throw new ArgumentNullException(nameof(documentSynchronizer));
            }

            if (razorLogger is null)
            {
                throw new ArgumentNullException(nameof(razorLogger));
            }

            if (loggerProvider is null)
            {
                throw new ArgumentNullException(nameof(loggerProvider));
            }

            _requestInvoker = requestInvoker;
            _documentSynchronizer = documentSynchronizer;
            _activityLogger = razorLogger;
            _loggerProvider = loggerProvider;
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

            // We initialize the logger here instead of the constructor as the projection provider is constructed
            // *before* the language server. Thus, the log hub has yet to be initialized, thus we would be unable to
            // create the logger at that time.
            InitializeLogHubLogger();

            var languageQueryParams = new RazorLanguageQueryParams()
            {
                Position = position,
                Uri = documentSnapshot.Uri
            };

            var languageResponse = await _requestInvoker.ReinvokeRequestOnServerAsync<RazorLanguageQueryParams, RazorLanguageQueryResponse>(
                LanguageServerConstants.RazorLanguageQueryEndpoint,
                RazorLSPConstants.RazorLSPContentTypeName,
                languageQueryParams,
                cancellationToken).ConfigureAwait(false);

            if (languageResponse == null)
            {
                _logHubLogger.LogInformation("The language server is still being spun up. Could not resolve the projection.");
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
                _logHubLogger.LogInformation($"Could not find projection for {languageResponse.Kind:G}.");
                return null;
            }

            if (languageResponse.HostDocumentVersion is null)
            {
                // There should always be a document version attached to an open document.
                // Log it and move on as if it was synchronized.
                var message = $"Could not find a document version associated with the document '{documentSnapshot.Uri}'";
                _activityLogger.LogVerbose(message);
                _logHubLogger.LogWarning(message);
            }
            else
            {
                var synchronized = await _documentSynchronizer.TrySynchronizeVirtualDocumentAsync(documentSnapshot.Version, virtualDocument, cancellationToken).ConfigureAwait(false);
                if (!synchronized)
                {
                    _logHubLogger.LogInformation("Could not synchronize.");
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

        private void InitializeLogHubLogger()
        {
            if (_logHubLogger is null)
            {
                _logHubLogger = _loggerProvider.CreateLogger(nameof(DefaultLSPProjectionProvider));
            }
        }
    }
}
