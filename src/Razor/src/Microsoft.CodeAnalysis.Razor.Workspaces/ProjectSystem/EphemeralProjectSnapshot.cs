// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class EphemeralProjectSnapshot : ProjectSnapshot
    {
        private readonly HostWorkspaceServices _services;
        private readonly Lazy<RazorProjectEngine> _projectEngine;

        public EphemeralProjectSnapshot(HostWorkspaceServices services, string filePath)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _services = services;
            FilePath = filePath;

            _projectEngine = new Lazy<RazorProjectEngine>(CreateProjectEngine);
        }

        public override RazorConfiguration Configuration => FallbackRazorConfiguration.Latest;

        public override IEnumerable<string> DocumentFilePaths => Array.Empty<string>();

        public override string FilePath { get; }

        public override string RootNamespace { get; }

        public override VersionStamp Version { get; } = VersionStamp.Default;

        public override IReadOnlyList<TagHelperDescriptor> TagHelpers { get; } = Array.Empty<TagHelperDescriptor>();

        public override DocumentSnapshot GetDocument(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return null;
        }

        public override bool IsImportDocument(DocumentSnapshot document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return false;
        }

        public override IEnumerable<DocumentSnapshot> GetRelatedDocuments(DocumentSnapshot document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return Array.Empty<DocumentSnapshot>();
        }

        public override RazorProjectEngine GetProjectEngine()
        {
            return _projectEngine.Value;
        }

        private RazorProjectEngine CreateProjectEngine()
        {
            var factory = _services.GetRequiredService<ProjectSnapshotProjectEngineFactory>();
            return factory.Create(this);
        }
    }
}
