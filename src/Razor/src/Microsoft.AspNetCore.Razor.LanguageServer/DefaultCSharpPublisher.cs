// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultCSharpPublisher : CSharpPublisher
    {
        private static readonly SourceText EmptySourceText = SourceText.From(string.Empty);
        private readonly Dictionary<string, SourceText> _publishedSourceText;
        private readonly Lazy<ILanguageServer> _server;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private ProjectSnapshotManagerBase _projectSnapshotManager;

        public DefaultCSharpPublisher(
            ForegroundDispatcher foregroundDispatcher,
            Lazy<ILanguageServer> server)
        {
            if (foregroundDispatcher is null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (server is null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _server = server;
            _publishedSourceText = new Dictionary<string, SourceText>(FilePathComparer.Instance);
        }

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            _projectSnapshotManager = projectManager;
            _projectSnapshotManager.Changed += ProjectSnapshotManager_Changed;
        }

        public override void Publish(string filePath, SourceText sourceText, long hostDocumentVersion)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (sourceText is null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            _foregroundDispatcher.AssertForegroundThread();

            if (!_publishedSourceText.TryGetValue(filePath, out var previouslyPublishedText))
            {
                previouslyPublishedText = EmptySourceText;
            }

            IReadOnlyList<TextChange> textChanges = Array.Empty<TextChange>();
            if (!sourceText.ContentEquals(previouslyPublishedText))
            {
                textChanges = sourceText.GetTextChanges(previouslyPublishedText);
            }

            _publishedSourceText[filePath] = sourceText;

            var request = new UpdateCSharpBufferRequest()
            {
                HostDocumentFilePath = filePath,
                Changes = textChanges,
                HostDocumentVersion = hostDocumentVersion,
            };

            _server.Value.Client.SendRequest("updateCSharpBuffer", request);
        }

        private void ProjectSnapshotManager_Changed(object sender, ProjectChangeEventArgs args)
        {
            _foregroundDispatcher.AssertForegroundThread();

            switch (args.Kind)
            {
                case ProjectChangeKind.DocumentChanged:
                case ProjectChangeKind.DocumentRemoved:
                    if (_publishedSourceText.ContainsKey(args.DocumentFilePath) &&
                        !_projectSnapshotManager.IsDocumentOpen(args.DocumentFilePath))
                    {
                        // Document closed or removed, evict published source text.
                        var removed = _publishedSourceText.Remove(args.DocumentFilePath);

                        Debug.Assert(removed, "Published source text should be protected by the foreground thread and should never fail to remove.");
                    }
                    break;
            }
        }
    }
}
