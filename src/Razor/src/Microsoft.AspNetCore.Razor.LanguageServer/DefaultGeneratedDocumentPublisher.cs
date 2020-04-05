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
        private readonly Dictionary<string, PublishData> _publishedCSharpData;
        private readonly Dictionary<string, PublishData> _publishedHtmlData;
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
            _publishedCSharpData = new Dictionary<string, PublishData>(FilePathComparer.Instance);
            _publishedHtmlData = new Dictionary<string, PublishData>(FilePathComparer.Instance);
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

            if (!_publishedCSharpData.TryGetValue(filePath, out var previouslyPublishedData))
            {
                previouslyPublishedData = PublishData.Default;
            }

            IReadOnlyList<TextChange> textChanges = Array.Empty<TextChange>();
            if (!sourceText.ContentEquals(previouslyPublishedData.SourceText))
            {
                textChanges = sourceText.GetTextChanges(previouslyPublishedData.SourceText);
            }
            else if (hostDocumentVersion == previouslyPublishedData.HostDocumentVersion)
            {
                // Source texts match along with host documetn versions. We've already published something that looks like this. No-op.
                return;
            }

            _publishedCSharpData[filePath] = new PublishData(sourceText, hostDocumentVersion);

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

            if (!_publishedHtmlData.TryGetValue(filePath, out var previouslyPublishedData))
            {
                previouslyPublishedData = PublishData.Default;
            }

            IReadOnlyList<TextChange> textChanges = Array.Empty<TextChange>();
            if (!sourceText.ContentEquals(previouslyPublishedData.SourceText))
            {
                textChanges = sourceText.GetTextChanges(previouslyPublishedData.SourceText);
            }
            else if (hostDocumentVersion == previouslyPublishedData.HostDocumentVersion)
            {
                // Source texts match along with host document versions. We've already published something that looks like this. No-op.
                return;
            }

            _publishedHtmlData[filePath] = new PublishData(sourceText, hostDocumentVersion);

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
                        if (_publishedCSharpData.ContainsKey(args.DocumentFilePath))
                        {
                            var removed = _publishedCSharpData.Remove(args.DocumentFilePath);
                            Debug.Assert(removed, "Published data should be protected by the foreground thread and should never fail to remove.");
                        }
                        if (_publishedHtmlData.ContainsKey(args.DocumentFilePath))
                        {
                            var removed = _publishedHtmlData.Remove(args.DocumentFilePath);
                            Debug.Assert(removed, "Published data should be protected by the foreground thread and should never fail to remove.");
                        }
                    }
                    break;
                case ProjectChangeKind.DocumentRemoved:
                    // Document removed, evict published source text.
                    if (_publishedCSharpData.ContainsKey(args.DocumentFilePath))
                    {
                        var removed = _publishedCSharpData.Remove(args.DocumentFilePath);
                        Debug.Assert(removed, "Published data should be protected by the foreground thread and should never fail to remove.");
                    }
                    if (_publishedHtmlData.ContainsKey(args.DocumentFilePath))
                    {
                        var removed = _publishedHtmlData.Remove(args.DocumentFilePath);
                        Debug.Assert(removed, "Published data should be protected by the foreground thread and should never fail to remove.");
                    }
                    break;
            }
        }

        private sealed class PublishData
        {
            public static readonly PublishData Default = new PublishData(SourceText.From(string.Empty), -1);

            public PublishData(SourceText sourceText, long hostDocumentVersion)
            {
                SourceText = sourceText;
                HostDocumentVersion = hostDocumentVersion;
            }

            public SourceText SourceText { get; }

            public long HostDocumentVersion { get; }
        }
    }
}
