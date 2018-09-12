// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultDocumentVersionCache : DocumentVersionCache
    {
        // Track the last 10 versions of the document
        internal const int MaxDocumentTrackingCount = 10;

        // Internal for testing
        internal readonly Dictionary<string, List<DocumentEntry>> _documentLookup;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private ProjectSnapshotManagerBase _projectSnapshotManager;

        public DefaultDocumentVersionCache(ForegroundDispatcher foregroundDispatcher)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentLookup = new Dictionary<string, List<DocumentEntry>>(FilePathComparer.Instance);
        }

        public override void TrackDocumentVersion(DocumentSnapshot documentSnapshot, long version)
        {
            if (documentSnapshot == null)
            {
                throw new ArgumentNullException(nameof(documentSnapshot));
            }

            if (version < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(version));
            }

            _foregroundDispatcher.AssertForegroundThread();

            if (!_documentLookup.TryGetValue(documentSnapshot.FilePath, out var documentEntries))
            {
                documentEntries = new List<DocumentEntry>();
                _documentLookup[documentSnapshot.FilePath] = documentEntries;
            }

            if (documentEntries.Count == MaxDocumentTrackingCount)
            {
                // Clear the oldest document entry

                // With this approach we'll slowly leak memory as new documents are added to the system. We don't clear up
                // document file paths where where all of the corresponding entries are expired.
                documentEntries.RemoveAt(0);
            }

            var entry = new DocumentEntry(documentSnapshot, version);
            documentEntries.Add(entry);
        }

        public override bool TryGetDocumentVersion(DocumentSnapshot documentSnapshot, out long version)
        {
            if (documentSnapshot == null)
            {
                throw new ArgumentNullException(nameof(documentSnapshot));
            }

            _foregroundDispatcher.AssertForegroundThread();

            if (!_documentLookup.TryGetValue(documentSnapshot.FilePath, out var documentEntries))
            {
                version = -1;
                return false;
            }

            var entry = documentEntries.Find(e => 
                e.Document.TryGetTarget(out var document) && document == documentSnapshot);
            if (entry == null)
            {
                version = -1;
                return false;
            }

            version = entry.Version;
            return true;
        }

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            _projectSnapshotManager = projectManager;
            _projectSnapshotManager.Changed += ProjectSnapshotManager_Changed;
        }

        private void ProjectSnapshotManager_Changed(object sender, ProjectChangeEventArgs args)
        {
            _foregroundDispatcher.AssertForegroundThread();

            switch (args.Kind)
            {
                case ProjectChangeKind.DocumentRemoved:
                case ProjectChangeKind.DocumentChanged:
                    if (_documentLookup.ContainsKey(args.DocumentFilePath) &&
                        !_projectSnapshotManager.IsDocumentOpen(args.DocumentFilePath))
                    {
                        // Document closed or removed, evict entry.
                        _documentLookup.Remove(args.DocumentFilePath);
                    }
                    break;
            }
        }

        internal class DocumentEntry
        {
            public DocumentEntry(DocumentSnapshot document, long version)
            {
                Document = new WeakReference<DocumentSnapshot>(document);
                Version = version;
            }

            public WeakReference<DocumentSnapshot> Document { get; }

            public long Version { get; }
        }
    }
}
