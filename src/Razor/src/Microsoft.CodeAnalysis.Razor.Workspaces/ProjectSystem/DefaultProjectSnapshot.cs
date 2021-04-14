// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class DefaultProjectSnapshot : ProjectSnapshot
    {
        private readonly object _lock;

        private readonly Dictionary<string, DefaultDocumentSnapshot> _documents;

        public DefaultProjectSnapshot(ProjectState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            State = state;

            _lock = new object();
            _documents = new Dictionary<string, DefaultDocumentSnapshot>(FilePathComparer.Instance);
        }

        public ProjectState State { get; }

        public override RazorConfiguration Configuration => HostProject.Configuration;

        public override IEnumerable<string> DocumentFilePaths => State.Documents.Keys;

        public override string FilePath => State.HostProject.FilePath;

        public override string RootNamespace => State.HostProject.RootNamespace;

        public override LanguageVersion CSharpLanguageVersion => State.CSharpLanguageVersion;

        public HostProject HostProject => State.HostProject;

        public override VersionStamp Version => State.Version;

        public override IReadOnlyList<TagHelperDescriptor> TagHelpers => State.TagHelpers;

        public override ProjectWorkspaceState ProjectWorkspaceState => State.ProjectWorkspaceState;

        public override DocumentSnapshot GetDocument(string filePath)
        {
            lock (_lock)
            {
                if (!_documents.TryGetValue(filePath, out var result) &&
                    State.Documents.TryGetValue(filePath, out var state))
                {
                    result = new DefaultDocumentSnapshot(this, state);
                    _documents.Add(filePath, result);
                }

                return result;
            }
        }

        public override bool IsImportDocument(DocumentSnapshot document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return State.ImportsToRelatedDocuments.ContainsKey(document.TargetPath);
        }

        public override IEnumerable<DocumentSnapshot> GetRelatedDocuments(DocumentSnapshot document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (State.ImportsToRelatedDocuments.TryGetValue(document.TargetPath, out var relatedDocuments))
            {
                lock (_lock)
                {
                    return relatedDocuments.Select(GetDocument).ToArray();
                }
            }

            return Array.Empty<DocumentSnapshot>();
        }

        public override RazorProjectEngine GetProjectEngine()
        {
            return State.ProjectEngine;
        }
    }
}
