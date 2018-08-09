// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test
{
    public class TestProjectSnapshot : ProjectSnapshotShim
    {
        private readonly Dictionary<string, DocumentSnapshotShim> _documents;

        public TestProjectSnapshot(string filePath) : this(filePath, new string[0])
        {
        }

        public TestProjectSnapshot(string filePath, string[] documentFilePaths)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (documentFilePaths == null)
            {
                throw new ArgumentNullException(nameof(documentFilePaths));
            }

            FilePath = filePath;
            HostProject = HostProjectShim.Create(filePath, RazorConfiguration.Default);
            DocumentFilePaths = documentFilePaths;
            Configuration = RazorConfiguration.Default;
            _documents = new Dictionary<string, DocumentSnapshotShim>();

            foreach (var documentFilePath in documentFilePaths)
            {
                _documents[documentFilePath] = new TestDocumentSnapshot(documentFilePath);
            }
        }

        public override HostProjectShim HostProject { get; }

        public override RazorConfiguration Configuration { get; }

        public override IEnumerable<string> DocumentFilePaths { get; }

        public override string FilePath { get; }

        public override bool IsInitialized => throw new NotImplementedException();

        public override VersionStamp Version => throw new NotImplementedException();

        public override Project WorkspaceProject => throw new NotImplementedException();

        public override DocumentSnapshotShim GetDocument(string filePath)
        {
            if (!_documents.TryGetValue(filePath, out var documentSnapshot))
            {
                throw new InvalidOperationException("Test was not setup correctly. Could not locate document '" + filePath + "'.");
            }

            return documentSnapshot;
        }

        public override RazorProjectEngine GetProjectEngine()
        {
            throw new NotImplementedException();
        }

        public override Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync() => Task.FromResult<IReadOnlyList<TagHelperDescriptor>>(Array.Empty<TagHelperDescriptor>());

        public override bool TryGetTagHelpers(out IReadOnlyList<TagHelperDescriptor> result)
        {
            result = Array.Empty<TagHelperDescriptor>();
            return true;
        }
    }
}
