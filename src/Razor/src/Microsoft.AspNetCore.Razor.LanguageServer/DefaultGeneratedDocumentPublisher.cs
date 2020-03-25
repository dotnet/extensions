// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultGeneratedDocumentPublisher : GeneratedDocumentPublisher
    {
        private static readonly SourceText EmptySourceText = SourceText.From(string.Empty);
        private readonly Dictionary<string, SourceText> _publishedCSharpSourceText;
        private readonly Dictionary<string, SourceText> _publishedHtmlSourceText;
        private readonly Lazy<ILanguageServer> _server;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private ProjectSnapshotManagerBase _projectSnapshotManager;

        public DefaultGeneratedDocumentPublisher(
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
            _publishedCSharpSourceText = new Dictionary<string, SourceText>(FilePathComparer.Instance);
            _publishedHtmlSourceText = new Dictionary<string, SourceText>(FilePathComparer.Instance);
        }

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            _projectSnapshotManager = projectManager;
            _projectSnapshotManager.Changed += ProjectSnapshotManager_Changed;
        }

        public override void PublishCSharp(string filePath, SourceText sourceText, long hostDocumentVersion)
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

            if (!_publishedCSharpSourceText.TryGetValue(filePath, out var previouslyPublishedText))
            {
                previouslyPublishedText = EmptySourceText;
            }

            IReadOnlyList<TextChange> textChanges = Array.Empty<TextChange>();
            if (!sourceText.ContentEquals(previouslyPublishedText))
            {
                textChanges = sourceText.GetTextChanges(previouslyPublishedText);
            }

            _publishedCSharpSourceText[filePath] = sourceText;

            var request = new UpdateBufferRequest()
            {
                HostDocumentFilePath = filePath,
                Changes = textChanges,
                HostDocumentVersion = hostDocumentVersion,
            };

            _server.Value.Client.SendRequest(LanguageServerConstants.RazorUpdateCSharpBufferEndpoint, request);
        }

        public override void PublishHtml(string filePath, SourceText sourceText, long hostDocumentVersion)
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

            if (!_publishedHtmlSourceText.TryGetValue(filePath, out var previouslyPublishedText))
            {
                previouslyPublishedText = EmptySourceText;
            }

            IReadOnlyList<TextChange> textChanges = Array.Empty<TextChange>();
            if (!sourceText.ContentEquals(previouslyPublishedText))
            {
                textChanges = sourceText.GetTextChanges(previouslyPublishedText);
            }

            _publishedHtmlSourceText[filePath] = sourceText;

            var request = new UpdateBufferRequest()
            {
                HostDocumentFilePath = filePath,
                Changes = textChanges,
                HostDocumentVersion = hostDocumentVersion,
            };

            _server.Value.Client.SendRequest(LanguageServerConstants.RazorUpdateHtmlBufferEndpoint, request);
        }

        private void ProjectSnapshotManager_Changed(object sender, ProjectChangeEventArgs args)
        {
            _foregroundDispatcher.AssertForegroundThread();

            switch (args.Kind)
            {
                case ProjectChangeKind.DocumentChanged:
                    if (!_projectSnapshotManager.IsDocumentOpen(args.DocumentFilePath))
                    {
                        // Document closed, evict published source text.
                        if (_publishedCSharpSourceText.ContainsKey(args.DocumentFilePath))
                        {
                            var removed = _publishedCSharpSourceText.Remove(args.DocumentFilePath);
                            Debug.Assert(removed, "Published source text should be protected by the foreground thread and should never fail to remove.");
                        }
                        if (_publishedHtmlSourceText.ContainsKey(args.DocumentFilePath))
                        {
                            var removed = _publishedHtmlSourceText.Remove(args.DocumentFilePath);
                            Debug.Assert(removed, "Published source text should be protected by the foreground thread and should never fail to remove.");
                        }
                    }
                    break;
                case ProjectChangeKind.DocumentRemoved:
                    // Document removed, evict published source text.
                    if (_publishedCSharpSourceText.ContainsKey(args.DocumentFilePath))
                    {
                        var removed = _publishedCSharpSourceText.Remove(args.DocumentFilePath);
                        Debug.Assert(removed, "Published source text should be protected by the foreground thread and should never fail to remove.");
                    }
                    if (_publishedHtmlSourceText.ContainsKey(args.DocumentFilePath))
                    {
                        var removed = _publishedHtmlSourceText.Remove(args.DocumentFilePath);
                        Debug.Assert(removed, "Published source text should be protected by the foreground thread and should never fail to remove.");
                    }
                    break;
            }
        }
    }
}
