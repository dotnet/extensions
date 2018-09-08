// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultHostDocumentFactory : HostDocumentFactory
    {
        private readonly ILanguageServer _router;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentVersionCache _documentVersionCache;

        public DefaultHostDocumentFactory(
            ForegroundDispatcher foregroundDispatcher,
            DocumentVersionCache documentVersionCache,
            ILanguageServer router)
        {

            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (documentVersionCache == null)
            {
                throw new ArgumentNullException(nameof(documentVersionCache));
            }

            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentVersionCache = documentVersionCache;
            _router = router;
        }

        public override HostDocument Create(string documentFilePath)
        {
            var hostDocument = new HostDocument(documentFilePath, documentFilePath);
            hostDocument.GeneratedCodeContainer.GeneratedCodeChanged += (sender, args) =>
            {
                var generatedCodeContainer = (GeneratedCodeContainer)sender;

                IReadOnlyList<TextChange> textChanges;

                if (args.NewText.ContentEquals(args.OldText))
                {
                    // If the content is equal then no need to update the underlying CSharp buffer.
                    textChanges = Array.Empty<TextChange>();
                }
                else
                {
                    textChanges = args.NewText.GetTextChanges(args.OldText);
                }

                var latestDocument = generatedCodeContainer.LatestDocument;

                Task.Factory.StartNew(() =>
                {
                    if (!_documentVersionCache.TryGetDocumentVersion(latestDocument, out var hostDocumentVersion))
                    {
                        // Cache entry doesn't exist, document most likely was evicted from the cache/too old.
                        return;
                    }

                    var request = new UpdateCSharpBufferRequest()
                    {
                        HostDocumentFilePath = documentFilePath,
                        Changes = textChanges,
                        HostDocumentVersion = hostDocumentVersion
                    };

                    _router.Client.SendRequest("updateCSharpBuffer", request);
                }, CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);
            };

            return hostDocument;
        }
    }
}
