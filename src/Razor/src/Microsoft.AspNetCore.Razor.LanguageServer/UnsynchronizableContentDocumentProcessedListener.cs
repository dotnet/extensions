// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class UnsynchronizableContentDocumentProcessedListener : DocumentProcessedListener
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentVersionCache _documentVersionCache;
        private readonly ILanguageServer _router;
        private ProjectSnapshotManager _projectManager;

        public UnsynchronizableContentDocumentProcessedListener(
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

        public override void DocumentProcessed(DocumentSnapshot document)
        {
            _foregroundDispatcher.AssertForegroundThread();

            if (!_projectManager.IsDocumentOpen(document.FilePath))
            {
                return;
            }

            if (!(document is DefaultDocumentSnapshot defaultDocument))
            {
                return;
            }

            if (!_documentVersionCache.TryGetDocumentVersion(document, out var syncVersion))
            {
                // Document is no longer important.
                return;
            }

            var latestSynchronizedDocument = defaultDocument.State.HostDocument.GeneratedCodeContainer.LatestDocument;
            if (latestSynchronizedDocument == null ||
                latestSynchronizedDocument == document)
            {
                // Already up-to-date
                return;
            }

            if (IdenticalOutputAfterParse(document, latestSynchronizedDocument, syncVersion))
            {
                // Documents are identical but we didn't synchronize them because they didn't need to be re-evaluated.

                var request = new UpdateCSharpBufferRequest()
                {
                    HostDocumentFilePath = document.FilePath,
                    Changes = Array.Empty<TextChange>(),
                    HostDocumentVersion = syncVersion
                };

                _router.Client.SendRequest("updateCSharpBuffer", request);
            }
        }

        public override void Initialize(ProjectSnapshotManager projectManager)
        {
            _projectManager = projectManager;
        }

        private bool IdenticalOutputAfterParse(DocumentSnapshot document, DocumentSnapshot latestSynchronizedDocument, long syncVersion)
        {
            return latestSynchronizedDocument.TryGetTextVersion(out var latestSourceVersion) &&
                document.TryGetTextVersion(out var documentSourceVersion) &&
                _documentVersionCache.TryGetDocumentVersion(latestSynchronizedDocument, out var lastSynchronizedVersion) &&
                syncVersion > lastSynchronizedVersion &&
                latestSourceVersion == documentSourceVersion;
        }
    }
}
