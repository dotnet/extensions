// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.LiveShare.Razor.Test
{
    internal class TestProjectSnapshot : ProjectSnapshot
    {
        private readonly TagHelperDescriptor[] _tagHelpers;

        public TestProjectSnapshot(string filePath, params TagHelperDescriptor[] tagHelpers)
        {
            FilePath = filePath;
            _tagHelpers = tagHelpers;
        }

        public override RazorConfiguration Configuration => RazorConfiguration.Default;

        public override IEnumerable<string> DocumentFilePaths => throw new NotImplementedException();

        public override string FilePath { get; }

        public override bool IsInitialized => throw new NotImplementedException();

        public override VersionStamp Version => throw new NotImplementedException();

        public override Project WorkspaceProject => throw new NotImplementedException();

        public override DocumentSnapshot GetDocument(string filePath)
        {
            throw new NotImplementedException();
        }

        public override RazorProjectEngine GetProjectEngine()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<DocumentSnapshot> GetRelatedDocuments(DocumentSnapshot document)
        {
            throw new NotImplementedException();
        }

        public override bool IsImportDocument(DocumentSnapshot document)
        {
            throw new NotImplementedException();
        }

        public override Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync()
        {
            return Task.FromResult((IReadOnlyList<TagHelperDescriptor>)_tagHelpers);
        }

        public override bool TryGetTagHelpers(out IReadOnlyList<TagHelperDescriptor> results)
        {
            results = _tagHelpers;
            return true;
        }
    }
}
