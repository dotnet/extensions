// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultGeneratedCodeContainerStore : GeneratedCodeContainerStore
    {
        private readonly ConcurrentDictionary<string, GeneratedCodeContainer> _store;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentVersionCache _documentVersionCache;
        private readonly CSharpPublisher _csharpPublisher;
        private ProjectSnapshotManagerBase _projectSnapshotManager;

        public DefaultGeneratedCodeContainerStore(
            ForegroundDispatcher foregroundDispatcher,
            DocumentVersionCache documentVersionCache,
            CSharpPublisher csharpPublisher)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (documentVersionCache == null)
            {
                throw new ArgumentNullException(nameof(documentVersionCache));
            }

            if (csharpPublisher is null)
            {
                throw new ArgumentNullException(nameof(csharpPublisher));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentVersionCache = documentVersionCache;
            _csharpPublisher = csharpPublisher;
            _store = new ConcurrentDictionary<string, GeneratedCodeContainer>(FilePathComparer.Instance);
        }

        public override GeneratedCodeContainer Get(string physicalFilePath)
        {
            if (physicalFilePath == null)
            {
                throw new ArgumentNullException(nameof(physicalFilePath));
            }

            lock (_store)
            {
                var codeContainer = _store.GetOrAdd(physicalFilePath, Create);
                return codeContainer;
            }
        }

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            _projectSnapshotManager = projectManager;
            _projectSnapshotManager.Changed += ProjectSnapshotManager_Changed;
        }

        // Internal for testing
        internal void ProjectSnapshotManager_Changed(object sender, ProjectChangeEventArgs args)
        {
            _foregroundDispatcher.AssertForegroundThread();

            switch (args.Kind)
            {
                case ProjectChangeKind.DocumentChanged:
                case ProjectChangeKind.DocumentRemoved:
                    lock (_store)
                    {
                        if (_store.ContainsKey(args.DocumentFilePath) &&
                            !_projectSnapshotManager.IsDocumentOpen(args.DocumentFilePath))
                        {
                            // Document closed or removed, evict entry.
                            _store.TryRemove(args.DocumentFilePath, out var _);
                        }
                    }
                    break;
            }
        }

        private GeneratedCodeContainer Create(string filePath)
        {
            var codeContainer = new GeneratedCodeContainer();
            codeContainer.GeneratedCodeChanged += (sender, args) =>
            {
                var generatedCodeContainer = (GeneratedCodeContainer)sender;

                var latestDocument = generatedCodeContainer.LatestDocument;

                Task.Factory.StartNew(() =>
                {
                    if (!_projectSnapshotManager.IsDocumentOpen(filePath))
                    {
                        // Document isn't opened, no need to notify the client
                        return;
                    }

                    if (!_documentVersionCache.TryGetDocumentVersion(latestDocument, out var hostDocumentVersion))
                    {
                        // Cache entry doesn't exist, document most likely was evicted from the cache/too old.
                        return;
                    }

                    _csharpPublisher.Publish(filePath, args.NewText, hostDocumentVersion);
                }, CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);
            };

            return codeContainer;
        }
    }
}
